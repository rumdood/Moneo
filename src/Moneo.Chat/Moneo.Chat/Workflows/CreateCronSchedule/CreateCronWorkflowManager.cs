using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

internal enum DayRepeatMode
{
    Undefined,
    Daily,
    SpecificDays,
    DayOfWeek,
    DayOfMonth,
}

public class CreateCronWorkflowManager : WorkflowManagerBase, ICreateCronWorkflowManager
{
    private readonly ILogger<CreateCronWorkflowManager> _logger;
    private readonly Dictionary<long, CronStateMachine> _chatStates = new();
    private readonly
        Dictionary<CronWorkflowState, Func<CronDraft, string, (bool Success, string? FailureMessage)>>
        _responseHandlers = new();

    private readonly Dictionary<CronWorkflowState, Func<CronDraft, MoneoCommandResult>> _responseStore = new();

    private static (bool Success, string? FailureMessage) HandleWaitingForDailyOrSpecific(CronDraft draft, string userInput)
    {
        draft.DayRepeatMode = (userInput.Equals("daily", StringComparison.OrdinalIgnoreCase))
            ? DayRepeatMode.Daily
            : DayRepeatMode.SpecificDays;
        return (true, null);
    }

    private static (bool Success, string? FailureMessage) HandleWaitingForWeekOrMonthDays(CronDraft draft, string userInput)
    {
        var repeatMode = userInput switch
        {
            "Days of the Week" => DayRepeatMode.DayOfWeek,
            "Days of the Month" => DayRepeatMode.DayOfMonth,
            _ => throw new InvalidOperationException("Week or Month expected")
        };

        draft.DayRepeatMode = repeatMode;
        return (true, null);
    }

    private static (bool Success, string? FailureMessage) HandleWaitingForDaysOfWeek(CronDraft draft, string userInput)
    {
        if (userInput.Equals("done", StringComparison.OrdinalIgnoreCase))
        {
            // we've finished this part of the workflow
            draft.IsDaysToRepeatComplete = true;
            return (true, null);
        }

        draft.AddRepeatDayOfWeek(userInput);
        return (true, null);
    }

    private static (bool Success, string? FailureMessage) HandleWaitingForDaysOfMonth(CronDraft draft, string userInput)
    {
        var dates = userInput.Split(',').Select(int.Parse);
        foreach (var date in dates)
        {
            draft.AddRepeatDayOfMonth(date);
        }

        draft.IsDaysToRepeatComplete = true;

        return (true, null);
    }

    private static (bool Success, string? FailureMessage) HandleWaitingForTimesOfDay(CronDraft draft, string userInput)
    {
        var times = userInput.Split(',').Select(x => x.Trim());

        foreach (var time in times)
        {
            draft.AddRepeatTime(time);
        }

        draft.IsTimesToRepeatComplete = true;
        return (true, null);
    }

    private void InitWorkflow()
    {
        _responseHandlers[CronWorkflowState.WaitingForDailyOrSpecific] = HandleWaitingForDailyOrSpecific;
        _responseHandlers[CronWorkflowState.WaitingForWeekOrMonthDays] = HandleWaitingForWeekOrMonthDays;
        _responseHandlers[CronWorkflowState.WaitingForDaysOfWeek] = HandleWaitingForDaysOfWeek;
        _responseHandlers[CronWorkflowState.WaitingForTimesOfDay] = HandleWaitingForTimesOfDay;
        _responseHandlers[CronWorkflowState.WaitingForDaysOfMonth] = HandleWaitingForDaysOfMonth;
    }

    private static MoneoCommandResult GetDailyOrSpecificResponse(CronDraft draft)
    {
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "When do you want this to repeat? this is a menu",
            MenuOptions = ["Daily", "Choose Days"]
        };
    }

    private static MoneoCommandResult GetWeeklyOrMonthlyResponse(CronDraft draft)
    {
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "Do you want this to repeat on specific days of the week or days of the month?",
            MenuOptions = ["Days of the Week", "Days of the Month"]
        };
    }

    private static MoneoCommandResult GetDaysOfWeekResponse(CronDraft draft)
    {
        var defaultOptions = new[]
            {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday", "Done"};

        var sendOptions = defaultOptions.Where(day =>
            !Enum.TryParse<DayOfWeek>(day, out var parsed) || !draft.DaysOfWeekToRepeat.Contains(parsed))
            .ToHashSet();

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "Select the days you want to repeat, or choose done",
            MenuOptions = sendOptions,
        };
    }

    private static MoneoCommandResult GetTimeOfDayResponse(CronDraft draft)
    {
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText =
                "What times do you want this to repeat? Make sure that all times include AM or PM or are in military time (e.g. 10:30, 14:30)"
        };
    }

    private static MoneoCommandResult GetDaysOfMonthResponse(CronDraft draft)
    {
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText =
                "Enter the days of the month you want to repeat on (e.g. \"1, 14\" to repeat on the first and fourteenth)"
        };
    }

    private void InitResponses()
    {
        _responseStore[CronWorkflowState.WaitingForDailyOrSpecific] = GetDailyOrSpecificResponse;
        _responseStore[CronWorkflowState.WaitingForDaysOfWeek] = GetDaysOfWeekResponse;
        _responseStore[CronWorkflowState.WaitingForTimesOfDay] = GetTimeOfDayResponse;
        _responseStore[CronWorkflowState.WaitingForWeekOrMonthDays] = GetWeeklyOrMonthlyResponse;
        _responseStore[CronWorkflowState.WaitingForDaysOfMonth] = GetDaysOfMonthResponse;
    }

    private MoneoCommandResult? GetResponseToState(CronWorkflowState state, CronDraft draft)
    {
        if (_responseStore.TryGetValue(state, out var response))
        {
            return response.Invoke(draft);
        }

        return null;
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(long chatId)
    {
        await Mediator.Send(new CreateCronWorkflowStartedEvent(chatId));

        // save the machine
        _chatStates[chatId] = new CronStateMachine();

        return await ContinueWorkflowAsync(chatId, "");
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput)
    {
        if (!_chatStates.TryGetValue(chatId, out var machine))
        {
            // we shouldn't be here
            await CompleteWorkflowAsync(chatId);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "I'm not sure what went wrong, but it was bad"
            };
        }

        _logger.LogDebug("CRON Manager processing user input of [{@Input}]", userInput);

        if (machine.CurrentState != CronWorkflowState.Start &&
            _responseHandlers.TryGetValue(machine.CurrentState, out var handler))
        {
            var result = handler.Invoke(machine.Draft, userInput);

            if (!result.Success)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = result.FailureMessage
                };
            }
        }

        var response = GetResponseToState(machine.GoToNext(), machine.Draft);

        if (machine.CurrentState == CronWorkflowState.Complete || response is null)
        {
            return await CompleteWorkflowAsync(chatId, machine.Draft);
        }

        return response;
    }

    private async Task<MoneoCommandResult> CompleteWorkflowAsync(long chatId, CronDraft? draft = null)
    {
        _chatStates.Remove(chatId);
        await Mediator.Send(new CreateCronWorkflowCompletedEvent(chatId, draft?.GenerateCronStatement() ?? ""));

        if (draft is null || draft.DayRepeatMode == DayRepeatMode.Undefined)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "Weird thing that shouldn't have happened?"
            };
        }
        
        return await Mediator.Send(new CreateTaskContinuationRequest(chatId, draft!.GenerateCronStatement()));
    }

    public CreateCronWorkflowManager(IMediator mediator, ILogger<CreateCronWorkflowManager> logger) : base(mediator)
    {
        _logger = logger;
        InitWorkflow();
        InitResponses();
    }
}

using MediatR;
using Moneo.Chat.BotResponses;
using Moneo.Chat.Commands;

namespace Moneo.Chat;

public interface IConfirmCommandWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(ConfirmCommandRequest request);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
}

public class ConfirmCommandWorkflowManager : WorkflowManagerBase, IConfirmCommandWorkflowManager
{
    private readonly Dictionary<long, string> _userCommandsLookup = new();

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput)
    {
        var confirmation = UserConfirmationHelper.GetConfirmation(userInput);

        if (confirmation == UserConfirmation.Unknown)
        {
            return new MoneoCommandResult()
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = BotResponseHelper.GetBotResponse(BotResponseType.RequestYesOrNo)
            };
        }
        
        // the workflow completes regardless of what the user says at this point. Either we confirm the command and
        // run it, or we're unclear on what they meant to do
        await _mediator.Send(new ConfirmCommandWorkflowCompletedEvent(chatId));

        if (confirmation == UserConfirmation.Negative)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = BotResponseHelper.GetBotResponse(BotResponseType.UnsureHowToProceed)
            };
        }

        var foundCommand = _userCommandsLookup.TryGetValue(chatId, out var command);
        _userCommandsLookup.Remove(chatId);

        if (!foundCommand || command is null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "I'm sorry, something went wrong. What was it you wanted to do?"
            };
        }

        // I don't like this part and user request management like this should probably be moved into another class to be called by the managers
        var context = CommandContext.Get(chatId, ChatState.ConfirmCommand, command);
        var userRequest = UserRequestFactory.GetUserRequest(context);

        if (userRequest is IRequest<MoneoCommandResult> request)
        {
            return await _mediator.Send(request);
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.Error,
            UserMessageText = $"Unknown command: {context.CommandKey}"
        };

    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(ConfirmCommandRequest request)
    {
        _userCommandsLookup[request.ConversationId] = $"{request.PotentialCommand} {request.PotentialArguments}";

        await _mediator.Send(new ConfirmCommandWorkflowStartedEvent(request.ConversationId));
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = $"Do you want to {request.PotentialCommand} {request.PotentialArguments}?"
        };
    }

    public ConfirmCommandWorkflowManager(IMediator mediator) : base(mediator)
    {
    }
}

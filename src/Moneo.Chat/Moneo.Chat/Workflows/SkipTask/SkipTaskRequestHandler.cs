using MediatR;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client.Models;

namespace Moneo.Chat.UserRequests;

internal class SkipTaskRequestHandler : IRequestHandler<SkipTaskRequest, MoneoCommandResult>
{
    private readonly IMediator _mediator;
    private readonly ITaskResourceManager _taskResourceManager;

    public SkipTaskRequestHandler(IMediator mediator, ITaskResourceManager taskResourceManager)
    {
        _mediator = mediator;
        _taskResourceManager = taskResourceManager;
    }
    
    public async Task<MoneoCommandResult> Handle(SkipTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.TaskName))
        {
            return await _mediator.Send(new ListTasksRequest(request.ConversationId, true), cancellationToken);
        }

        var tasksMatchingName =
            await _taskResourceManager.GetTasksForUserAsync(request.ConversationId,
                new MoneoTaskFilter {SearchString = request.TaskName});

        if (!tasksMatchingName.IsSuccessful || !tasksMatchingName.Result.Any())
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"You don't have a task called {request.TaskName} or anything like it"
            };
        }

        var tasks = tasksMatchingName.Result.ToArray();

        if (tasks.Length == 1)
        {
            // here we'll do a call to the Azure Function to complete the task
            var skipTaskResult = await _taskResourceManager.CompleteTaskAsync(request.ConversationId, tasks.First().Id);

            return new MoneoCommandResult
            {
                ResponseType = skipTaskResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
                Type = skipTaskResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
                UserMessageText = skipTaskResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
            };
        }
        
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "There were multiple possible tasks that matched the description you gave",
            MenuOptions = tasks.Select(t => $"/skip {t.Name}").ToHashSet()
        };
    }
}

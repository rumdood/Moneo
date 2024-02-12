using MediatR;
using MediatR.Pipeline;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client.Models;

namespace Moneo.Chat.UserRequests;

internal class CompleteTaskRequestHandler : IRequestHandler<CompleteTaskRequest, MoneoCommandResult>
{
    private readonly IMediator _mediator;
    private readonly ITaskResourceManager _taskResourceManager;
    
    public CompleteTaskRequestHandler(IMediator mediator, ITaskResourceManager taskResourceManager)
    {
        _mediator = mediator;
        _taskResourceManager = taskResourceManager;
    }
    
    public async Task<MoneoCommandResult> Handle(CompleteTaskRequest request, CancellationToken cancellationToken)
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
            var completeTaskResult = await _taskResourceManager.CompleteTaskAsync(request.ConversationId, tasks.First().Id);

            return new MoneoCommandResult
            {
                ResponseType = completeTaskResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
                Type = completeTaskResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
                UserMessageText = completeTaskResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
            };
        }
        
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "There were multiple possible tasks that matched the description you gave",
            MenuOptions = tasks.Select(t => $"/complete {t.Name}").ToHashSet()
        };
    }
}

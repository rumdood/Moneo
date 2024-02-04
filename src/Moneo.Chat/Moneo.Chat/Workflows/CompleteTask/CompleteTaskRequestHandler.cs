using MediatR;
using MediatR.Pipeline;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;

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
        
        // here we'll do a call to the Azure Function to complete the task
        var completeTaskResult = await _taskResourceManager.CompleteTaskAsync(request.ConversationId, request.TaskName);

        return new MoneoCommandResult
        {
            ResponseType = completeTaskResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
            Type = completeTaskResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
            UserMessageText = completeTaskResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
        };
    }
}

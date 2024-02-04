using MediatR;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;

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
            if (string.IsNullOrEmpty(request.TaskName))
            {
                return await _mediator.Send(new ListTasksRequest(request.ConversationId, true), cancellationToken);
            }
        }
        
        // here we'll do a call to the Azure Function to complete the task
        var skipResult = await _taskResourceManager.SkipTaskAsync(request.ConversationId, request.TaskName);

        return new MoneoCommandResult
        {
            ResponseType = skipResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
            Type = skipResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
            UserMessageText = skipResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
        };
    }
}

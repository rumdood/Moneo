using MediatR;
using Moneo.Chat.Commands;
using Moneo.TaskManagement;

namespace Moneo.Chat.UserRequests;

internal class CompleteTaskRequestHandler : IRequestHandler<CompleteTaskRequest, MoneoCommandResult>
{
    private readonly ITaskResourceManager _taskResourceManager;
    
    public CompleteTaskRequestHandler(ITaskResourceManager taskResourceManager)
    {
        _taskResourceManager = taskResourceManager;
    }
    
    public async Task<MoneoCommandResult> Handle(CompleteTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.TaskName))
        {
            // TODO: Change this to retrieve a list of tasks and send them as a menu
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.NeedMoreInfo,
                UserMessageText = "You didn't tell me what task to complete. Send \"/complete taskName\""
            };
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

using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.UserRequests;

internal class CompleteTaskRequestHandler : IRequestHandler<CompleteTaskRequest, MoneoCommandResult>
{
    private readonly ITaskService _taskService;
    
    public CompleteTaskRequestHandler(ITaskService taskService)
    {
        _taskService = taskService;
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
        var proxyResult = await _taskService.CompleteTaskAsync(request.ConversationId, request.TaskName);

        return new MoneoCommandResult
        {
            ResponseType = proxyResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
            Type = proxyResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
            UserMessageText = proxyResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
        };
    }
}

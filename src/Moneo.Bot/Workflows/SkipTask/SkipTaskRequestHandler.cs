using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.UserRequests;

public class SkipTaskRequestHandler : IRequestHandler<SkipTaskRequest, MoneoCommandResult>
{
    private readonly IMoneoProxy _proxy;

    public SkipTaskRequestHandler(IMoneoProxy proxy)
    {
        _proxy = proxy;
    }
    
    public async Task<MoneoCommandResult> Handle(SkipTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.TaskName))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.NeedMoreInfo,
                UserMessageText = "You didn't tell me what task to skip. Send \"/skip taskName\""
            };
        }
        
        // here we'll do a call to the Azure Function to complete the task
        var proxyResult = await _proxy.SkipTaskAsync(request.ConversationId, request.TaskName);

        return new MoneoCommandResult
        {
            ResponseType = proxyResult.IsSuccessful ? ResponseType.None : ResponseType.Text,
            Type = proxyResult.IsSuccessful ? ResultType.WorkflowCompleted : ResultType.Error,
            UserMessageText = proxyResult.IsSuccessful ? "" : "Something went wrong. Look at the logs?"
        };
    }
}
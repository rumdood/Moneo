using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.UserRequests;

public class SkipTaskRequestHandler : IRequestHandler<SkipTaskRequest, MoneoCommandResult>
{
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

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = $"Skipping {request.TaskName}, we can always try again later"
        };
    }
}
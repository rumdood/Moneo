using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.UserRequests;

internal class CompleteTaskRequestHandler : IRequestHandler<CompleteTaskRequest, MoneoCommandResult>
{
    public async Task<MoneoCommandResult> Handle(CompleteTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.TaskName))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.NeedMoreInfo,
                UserMessageText = "You didn't tell me what task to complete. Send \"/complete taskName\""
            };
        }
        
        // here we'll do a call to the Azure Function to complete the task

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = "Well done!"
        };
    }
}

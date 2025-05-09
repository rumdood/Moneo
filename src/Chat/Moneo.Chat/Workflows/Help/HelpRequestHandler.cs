using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.Chitchat;

internal partial class HelpRequestHandler : IRequestHandler<HelpRequest, MoneoCommandResult>
{
    public Task<MoneoCommandResult> Handle(HelpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UserText))
        {
            return Task.FromResult(new MoneoCommandResult()
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = HelpResponseFactory.GetDefaultHelpResponse()
            });
        }

        var responseText = HelpResponseFactory.GetHelpResponse(request.UserText.Replace("/", ""));
        return Task.FromResult(new MoneoCommandResult()
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = responseText ?? HelpResponseFactory.GetDefaultHelpResponse()
        });
    }
}

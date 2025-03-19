using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.Chitchat;

internal partial class HelpRequestHandler : IRequestHandler<HelpRequest, MoneoCommandResult>
{
    public async Task<MoneoCommandResult> Handle(HelpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UserText))
        {
            return new MoneoCommandResult()
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = HelpResponseFactory.DefaultHelpResponse
            };
        }

        var responseText = HelpResponseFactory.GetHelpResponse(request.UserText.Replace("/", ""));
        return new MoneoCommandResult()
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = responseText ?? HelpResponseFactory.DefaultHelpResponse
        };
    }
}
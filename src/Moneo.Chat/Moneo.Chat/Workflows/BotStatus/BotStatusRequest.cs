using MediatR;
using Moneo.Chat.Commands;
using System.Text;

namespace Moneo.Chat.Workflows.BotStatus
{
    [UserCommand(CommandKey = "/botstatus", HelpDescription = @"Gets the status of the bot")]
    public partial class BotStatusRequest : UserRequestBase
    {
        public BotStatusRequest(long conversationId, params string[] args) : base(conversationId, args)
        {
        }
    }

    internal class BotStatusRequestHandler : IRequestHandler<BotStatusRequest, MoneoCommandResult>
    {
        private readonly IChatAdapter _chatAdapter;

        public BotStatusRequestHandler(IChatAdapter chatAdapter)
        {
            _chatAdapter = chatAdapter;
        }

        public async Task<MoneoCommandResult> Handle(BotStatusRequest request, CancellationToken cancellationToken)
        {
            var status = await _chatAdapter.GetStatusAsync(cancellationToken);

            var builder = new StringBuilder();
            builder.AppendLine("Bot Status:");
            builder.AppendLine($"Bot Adapter: {status.NameOfAdapter}");
            builder.AppendLine($"Using Webhook: {status.IsUsingWebhook}");
            
            if (status.WebHookInfo is not null)
            {
                builder.AppendLine($"Webhook Url: {status.WebHookInfo.Url}");
                builder.AppendLine($"Webhook Last Error Date: {status.WebHookInfo.LastErrorDate}");
                builder.AppendLine($"Webhook Last Error Message: {status.WebHookInfo.LastErrorMessage}");
                builder.AppendLine($"Webhook Pending Update Count: {status.WebHookInfo.PendingUpdateCount}");
            }

            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = builder.ToString()
            };
        }
    }
}

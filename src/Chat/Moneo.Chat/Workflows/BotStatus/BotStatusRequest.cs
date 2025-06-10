using MediatR;
using Moneo.Chat.Commands;
using System.Text;

namespace Moneo.Chat.Workflows.BotStatus
{
    [UserCommand(CommandKey = "/botstatus", HelpDescription = @"Gets the status of the bot")]
    public partial class BotStatusRequest : UserRequestBase
    {
        public BotStatusRequest(CommandContext context) : base(context)
        {
        }
    }

    internal class BotStatusRequestHandler : IRequestHandler<BotStatusRequest, MoneoCommandResult>
    {
        private readonly IChatAdapter _chatAdapter;
        private readonly IChatStateRepository _chatStateRepository;

        public BotStatusRequestHandler(IChatAdapter chatAdapter, IChatStateRepository chatStateRepository)
        {
            _chatAdapter = chatAdapter;
            _chatStateRepository = chatStateRepository;
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
            
            builder.AppendLine("==========================");
            builder.AppendLine("Chat States:");
            var chatStates = await _chatStateRepository.GetAllChatStatesAsync();
            if (chatStates.Count == 0)
            {
                builder.AppendLine("No chat states found.");
            }
            else
            {
                foreach (var chatState in chatStates)
                {
                    builder.AppendLine($"Chat ID: {chatState.ChatId}, User ID: {chatState.UserId}, State: {chatState.State}");
                }
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

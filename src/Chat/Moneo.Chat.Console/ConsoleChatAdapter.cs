using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Models;
using Moneo.Common;

namespace Moneo.Chat
{
    public record ConsoleUserMessage(long Id, long ConversationId, string Text);

    public class ConsoleChatAdapter(
        IBotClientConfiguration configuration, 
        IChatManager chatManager, 
        ILogger<ConsoleChatAdapter> logger) : IChatAdapter<ConsoleUserMessage, BotTextMessageRequest>,
        IRequestHandler<BotTextMessageRequest>,
        IRequestHandler<BotGifMessageRequest>,
        IRequestHandler<BotMenuMessageRequest>
    {
        private readonly IBotClientConfiguration _configuration = configuration;
        private readonly IChatManager _conversationManager = chatManager;
        private readonly ILogger<ConsoleChatAdapter> _logger = logger;

        public bool IsActive { get; private set; } = false;

        public Task<ChatAdapterStatus> GetStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChatAdapterStatus(nameof(ConsoleChatAdapter), false));
        }

        public Task Handle(BotTextMessageRequest request, CancellationToken cancellationToken)
        {
            Console.WriteLine("MARLOWE>>> " + request.Text, ConsoleColor.White);
            return Task.CompletedTask;
        }

        public Task Handle(BotGifMessageRequest request, CancellationToken cancellationToken)
        {
            Console.WriteLine("MARLOWE>>> Gif: " + request.GifUrl, ConsoleColor.White);
            return Task.CompletedTask;
        }

        public Task Handle(BotMenuMessageRequest request, CancellationToken cancellationToken)
        {
            Console.WriteLine("MARLOWE>>> " + request.Text, ConsoleColor.White);
            foreach (var menuItem in request.MenuOptions)
            {
                Console.WriteLine(menuItem, ConsoleColor.White);
            }

            return Task.CompletedTask;
        }

        public async Task ReceiveMessageAsync(ConsoleUserMessage message, CancellationToken cancellationToken)
        {
            // get the current windows user name
            var user = new ChatUser(0, Environment.UserName);

            try
            {
                await _conversationManager.ProcessUserMessageAsync(
                    new UserMessage(
                        message.ConversationId, user, message.Text)
                    );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An Error Occurred");
                Console.WriteLine($"ERROR: {e.Message}", ConsoleColor.Red);
            }
        }

        public Task ReceiveUserMessageAsJsonAsync(string message, CancellationToken cancellationToken)
        {
            var userMessage = JsonSerializer.Deserialize<ConsoleUserMessage>(message);
            
            if (userMessage is null)
            {
                throw new UserMessageFormatException("UserMessage is not in the correct format (expected ConsoleUserMessage)");
            }
            
            return ReceiveMessageAsync(userMessage, cancellationToken);
        }

        public Task ReceiveUserMessageAsync(object message, CancellationToken cancellationToken) =>
            ReceiveMessageAsync((ConsoleUserMessage)message, cancellationToken);

        public async Task SendBotGifMessageAsync(IBotGifMessage botGifMessage, CancellationToken cancellationToken)
        {
            if (botGifMessage is not BotGifMessageRequest message)
            {
                throw new UserMessageFormatException("BotGifMessage is not in the correct format");
            }

            await Handle(message, cancellationToken);
        }

        public async Task SendBotTextMessageAsync(IBotTextMessage botTextMessage, CancellationToken cancellationToken)
        {
            if (botTextMessage is not BotTextMessageRequest message)
            {
                throw new UserMessageFormatException("BotTextMessage is not in the correct format");
            }

            await Handle(message, cancellationToken);
        }

        public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Starting Marlow Console-Based Chat Adapter");
            Console.WriteLine("Press 'q' to quit");
            IsActive = true;

            while (IsActive)
            {
                Console.Write("MARLOWE:> ", ConsoleColor.Cyan);
                var input = Console.ReadLine();
                if (input == "q")
                {
                    await StopReceivingAsync(cancellationToken);
                    break;
                }
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("ERROR: Please enter a message", ConsoleColor.Red);
                    continue;
                }

                var conversationId = _configuration.MasterConversationId == 0 ? 1 : _configuration.MasterConversationId;

                await ReceiveMessageAsync(
                    new ConsoleUserMessage(DateTime.UtcNow.Ticks, conversationId, input), cancellationToken);
            }
        }

        public Task StartReceivingAsync(string callbackUrl, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopReceivingAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Stopping Marlow Console-Based Chat Adapter", ConsoleColor.Yellow);
            IsActive = false;
            return Task.CompletedTask;
        }
    }
}

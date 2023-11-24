using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Models;
using Moneo.Core;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Moneo.Chat.Telegram;

public class TelegramChatAdapter : IChatAdapter<Update, BotTextMessageRequest>, 
    IRequestHandler<BotTextMessageRequest>,
    IRequestHandler<BotGifMessageRequest>
{
    private readonly IBotClientConfiguration _configuration;
    private readonly ITelegramBotClient _botClient;
    private readonly IConversationManager _conversationManager;
    private readonly ILogger<TelegramChatAdapter> _logger;

    public TelegramChatAdapter(IBotClientConfiguration configuration, IConversationManager conversationManager,
        ILogger<TelegramChatAdapter> logger)
    {
        _logger = logger;
        _configuration = configuration;
        _botClient = new TelegramBotClient(configuration.Token);
        _conversationManager = conversationManager;
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is { } message && !string.IsNullOrEmpty(message.Text))
        {
            try
            {
                await _conversationManager.ProcessUserMessageAsync(new UserMessage(message.Chat.Id, message.Text,
                    message.Chat.FirstName ?? message.Chat.Username!, message.Chat.LastName));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An Error Occurred");
            }
        }
    }

    private async Task HandleErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancelToken)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            await _botClient.SendTextMessageAsync(_configuration.MasterConversationId, apiRequestException.ToString(),
                cancellationToken: cancelToken);
        }
    }

    public void StartReceiving(CancellationToken cancellationToken = default)
    {
        var options = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            ThrowPendingUpdates = true,
        };
        
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, options, cancellationToken);
    }

    public Task ReceiveMessageAsync(object message, CancellationToken cancellationToken)
    {
        var update = message as Update;

        if (message is null)
        {
            throw new InvalidOperationException("Message type is not supported by Telegram");
        }

        return ReceiveMessageAsync(update!, cancellationToken);
    }

    public Task ReceiveMessageAsync(Update message, CancellationToken cancellationToken) =>
        HandleUpdateAsync(_botClient, message, cancellationToken);

    public async Task Handle(BotTextMessageRequest request, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(request.ConversationId, request.Text, cancellationToken: cancellationToken);
    }

    public async Task Handle(BotGifMessageRequest request, CancellationToken cancellationToken)
    {
        var inputFile = new InputFileUrl(request.GifUrl);
        await _botClient.SendAnimationAsync(request.ConversationId, inputFile, cancellationToken: cancellationToken);
    }
}
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
        ILogger<TelegramChatAdapter> logger, ITelegramBotClient? botClient = null)
    {
        _logger = logger;
        _configuration = configuration;
        _botClient = botClient ?? new TelegramBotClient(configuration.BotToken);
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

    public async Task StartReceivingAsync(string callbackUrl, CancellationToken cancellationToken = default)
    {
        await _botClient.SetWebhookAsync(url: callbackUrl, secretToken: _configuration.CallbackToken, cancellationToken: cancellationToken);
    }

    public Task ReceiveUserMessageAsync(object message, CancellationToken cancellationToken)
    {
        if (message is not Update update)
        {
            throw new UserMessageFormatException("Message type is not supported by Telegram (expecting Update)");
        }

        return ReceiveMessageAsync(update, cancellationToken);
    }

    public async Task SendBotTextMessageAsync(IBotTextMessage botTextMessage, CancellationToken cancellationToken)
    {
        if (botTextMessage is not BotTextMessageRequest message)
        {
            throw new UserMessageFormatException("BotTextMessage is not in the correct format");
        }
        
        await Handle(message, cancellationToken);
    }

    public async Task SendBotGifMessageAsync(IBotGifMessage botGifMessage, CancellationToken cancellationToken)
    {
        if (botGifMessage is not BotGifMessageRequest message)
        {
            throw new UserMessageFormatException("BotGifMessage is not in the correct format");
        }
        
        await Handle(message, cancellationToken);
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
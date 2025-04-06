using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Moneo.Chat.Telegram;

public class TelegramChatAdapter : IChatAdapter<Update, BotTextMessageRequest>, 
    IRequestHandler<BotTextMessageRequest>,
    IRequestHandler<BotGifMessageRequest>,
    IRequestHandler<BotMenuMessageRequest>
{
    private readonly TelegramChatAdapterOptions _options;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatManager _conversationManager;
    private readonly ILogger<TelegramChatAdapter> _logger;
    private bool _isUsingWebhook = false;

    public TelegramChatAdapter(TelegramChatAdapterOptions options, IChatManager conversationManager,
        ILogger<TelegramChatAdapter> logger, ITelegramBotClient? botClient = null)
    {
        _logger = logger;
        _options = options;
        _botClient = botClient ?? new TelegramBotClient(options.BotToken);
        _conversationManager = conversationManager;
    }
    
    public bool IsActive { get; private set; } = false;

    private async Task DeleteExistingWebhookAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.DeleteWebhook(cancellationToken: cancellationToken);
            _isUsingWebhook = false;
        }
        catch (ApiRequestException e)
        {
            _logger.LogError(e, "Error occurred while deleting existing webhook");
        }
    }

    private async Task HandleMessageUpdateAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            await _conversationManager.ProcessUserMessageAsync(new UserMessage(message.Chat.Id, message.Text!,
                message.Chat.FirstName ?? message.Chat.Username!, message.Chat.LastName));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An Error Occurred");
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received callback query: {@Data}", callbackQuery.Data);
        await _conversationManager.ProcessUserMessageAsync(new UserMessage(callbackQuery.Message?.Chat.Id ?? 0,
            callbackQuery.Data!, callbackQuery.From.FirstName ?? callbackQuery.From.Username!));
    }

    private Task HandleUnknownUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Unknown update received");
        return Task.CompletedTask;
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing update: {@Update}", update);
        
        var handler = update switch
        {
            {Message: { } message} => HandleMessageUpdateAsync(message, cancellationToken),
            {CallbackQuery: { } callbackQuery} => HandleCallbackQueryAsync(callbackQuery, cancellationToken),
            _ => HandleUnknownUpdateAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task HandleErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancelToken)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            await _botClient.SendMessage(_options.MasterConversationId, apiRequestException.ToString(),
                cancellationToken: cancelToken);
        }
    }

    public Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        _isUsingWebhook = false;
        
        _botClient.Timeout = TimeSpan.FromSeconds(30);

        var options = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DropPendingUpdates = true
        };
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, options, cancellationToken);
        IsActive = true;

        return Task.CompletedTask;
    }

    public async Task StartReceivingAsync(string callbackUrl, CancellationToken cancellationToken = default)
    {
        _isUsingWebhook = true;
        await _botClient.SetWebhook(url: callbackUrl, secretToken: _options.CallbackToken, cancellationToken: cancellationToken);
        IsActive = true;
    }

    public async Task StopReceivingAsync(CancellationToken cancellationToken = default)
    {
        if (_isUsingWebhook)
        {
            await DeleteExistingWebhookAsync(cancellationToken);
        } 

        IsActive = false;
    }

    public async Task<ChatAdapterStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var webhookInfo = await _botClient.GetWebhookInfo(cancellationToken);
        return new ChatAdapterStatus(
            nameof(TelegramChatAdapter),
            _isUsingWebhook,
            new WebhookInfo(Url: webhookInfo.Url,
            LastErrorDate: webhookInfo.LastErrorDate,
            LastErrorMessage: webhookInfo.LastErrorMessage,
            PendingUpdateCount: webhookInfo.PendingUpdateCount));
    }

    public Task ReceiveUserMessageAsync(object message, CancellationToken cancellationToken)
    {
        if (message is not Update update)
        {
            throw new UserMessageFormatException("Message type is not supported by Telegram (expecting Update)");
        }

        return ReceiveMessageAsync(update, cancellationToken);
    }
    
    public Task ReceiveUserMessageAsJsonAsync(string json, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received JSON: {Json}", json);
        var update = JsonSerializer.Deserialize<Update>(json);

        if (update is not null)
        {
            return ReceiveMessageAsync(update, cancellationToken);
        }
        
        _logger.LogWarning("Failed to deserialize JSON");
        throw new UserMessageFormatException("Message type is not supported by Telegram (expecting Update)");
    }

    public async Task SendBotTextMessageAsync(IBotTextMessage botTextMessage, CancellationToken cancellationToken)
    {
        var message = botTextMessage as BotTextMessageRequest ??
                      new BotTextMessageRequest(
                          botTextMessage.ConversationId,
                          botTextMessage.Text,
                          botTextMessage.IsError);
        
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
        await _botClient.SendMessage(request.ConversationId, request.Text, cancellationToken: cancellationToken);
    }

    public async Task Handle(BotGifMessageRequest request, CancellationToken cancellationToken)
    {
        var inputFile = new InputFileUrl(request.GifUrl);
        await _botClient.SendAnimation(request.ConversationId, inputFile, cancellationToken: cancellationToken);
    }

    public async Task Handle(BotMenuMessageRequest request, CancellationToken cancellationToken)
    {
        var rows = request.MenuOptions.Chunk(2).ToArray();
        await _botClient.SendMessage(
            chatId: request.ConversationId, 
            text: request.Text, 
            replyMarkup: rows, 
            cancellationToken: cancellationToken);
    }

    private async Task HandleViaCallbackButton(BotMenuMessageRequest request, CancellationToken cancellationToken)
    {
        var options = request.MenuOptions.Select(InlineKeyboardButton.WithCallbackData).ToArray();

        IEnumerable<IEnumerable<InlineKeyboardButton>> GetRows(ICollection<InlineKeyboardButton> buttons,
            int maxRowSize)
        {
            var queue = new Queue<InlineKeyboardButton>(buttons);
            var currentRow = new List<InlineKeyboardButton>();

            while (queue.TryPeek(out _))
            {
                if (currentRow.Count < maxRowSize)
                {
                    currentRow.Add(queue.Dequeue());
                    continue;
                }

                yield return currentRow;
                currentRow = [];
            }

            if (currentRow.Count > 0)
            {
                yield return currentRow;
            }
        }

        var keyboard = new InlineKeyboardMarkup(GetRows(options, 2));
        await _botClient.SendMessage(chatId: request.ConversationId, text: request.Text, replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
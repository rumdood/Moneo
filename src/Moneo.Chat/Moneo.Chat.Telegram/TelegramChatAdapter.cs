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
using Telegram.Bot.Types.ReplyMarkups;

namespace Moneo.Chat.Telegram;

/*
"update_id": 492336862, "message": {
  "message_id": 8610,
  "from": {
    "id": 122243374,
    "is_bot": false,
    "first_name": "RumDood",
    "username": "rumdood",
    "language_code": "en"
  },
  "chat": {
    "id": 122243374,
    "first_name": "RumDood",
    "username": "rumdood",
    "type": "private"
  },
  "date": 1711059807,
  "text": "Hello?"
}
*/

public class TelegramChatAdapter : IChatAdapter<Update, BotTextMessageRequest>, 
    IRequestHandler<BotTextMessageRequest>,
    IRequestHandler<BotGifMessageRequest>,
    IRequestHandler<BotMenuMessageRequest>
{
    private readonly IBotClientConfiguration _configuration;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatManager _conversationManager;
    private readonly ILogger<TelegramChatAdapter> _logger;
    private bool _isUsingWebhook = false;

    public TelegramChatAdapter(IBotClientConfiguration configuration, IChatManager conversationManager,
        ILogger<TelegramChatAdapter> logger, ITelegramBotClient? botClient = null)
    {
        _logger = logger;
        _configuration = configuration;
        _botClient = botClient ?? new TelegramBotClient(configuration.BotToken);
        _conversationManager = conversationManager;
    }

    private async Task DeleteExistingWebhook(CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
            _isUsingWebhook = false;
        }
        catch (ApiRequestException e)
        {
            _logger.LogError(e, "Error occurred while deleting existing webhook");
        }
    }

    private async Task HandleMessageUpdate(Message message, CancellationToken cancellationToken)
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
        var handler = update switch
        {
            {Message: { } message} => HandleMessageUpdate(message, cancellationToken),
            {CallbackQuery: { } callbackQuery} => HandleCallbackQueryAsync(callbackQuery, cancellationToken),
            _ => HandleUnknownUpdateAsync(update, cancellationToken)
        };

        await handler;
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
        _isUsingWebhook = true;
        await _botClient.SetWebhookAsync(url: callbackUrl, secretToken: _configuration.CallbackToken, cancellationToken: cancellationToken);
    }

    public async Task StopReceivingAsync(CancellationToken cancellationToken = default)
    {
        if (_isUsingWebhook)
        {
            await DeleteExistingWebhook(cancellationToken);
        }
    }

    public async Task<ChatAdapterStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var webhookInfo = await _botClient.GetWebhookInfoAsync(cancellationToken);
        return new ChatAdapterStatus(nameof(TelegramChatAdapter), _isUsingWebhook, new WebhookInfo(Url: webhookInfo.Url,
                       LastErrorDate: webhookInfo.LastErrorDate, LastErrorMessage: webhookInfo.LastErrorMessage,
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

    public async Task Handle(BotMenuMessageRequest request, CancellationToken cancellationToken)
    {
        var options = request.MenuOptions.Select(InlineKeyboardButton.WithCallbackData).ToArray();

        IEnumerable<IEnumerable<InlineKeyboardButton>> GetRows(ICollection<InlineKeyboardButton> buttons,
            int maxRowSize)
        {
            var queue = new Queue<InlineKeyboardButton>(buttons);
            var currentRow = new List<InlineKeyboardButton>();

            while (queue.TryPeek(out _) || currentRow.Count > 0)
            {
                if (currentRow.Count < maxRowSize)
                {
                    currentRow.Add(queue.Dequeue());
                    continue;
                }

                yield return currentRow;
                currentRow = [];
            }
        }

        var keyboard = new InlineKeyboardMarkup(GetRows(options, 2));
        await _botClient.SendTextMessageAsync(chatId: request.ConversationId, text: request.Text, replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
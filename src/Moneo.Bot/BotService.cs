using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moneo.Bot.BotRequests;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Moneo.Bot;

internal class BotService : BackgroundService, IRequestHandler<BotTextMessageRequest>, IRequestHandler<BotGifMessageRequest>
{
    private readonly BotClientConfiguration _config;
    private readonly IConversationManager _conversationManager;
    private readonly TelegramBotClient _botClient;
    private readonly ITaskService _taskService;
    private readonly ILogger<BotService> _logger;
    
    private void RunBot(CancellationToken cancelToken)
    {
        var options = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            ThrowPendingUpdates = true,
        };
        
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, options, cancelToken);
        _logger.LogInformation("Moneo Bot Service Is Running");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancelToken)
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
            await _botClient.SendTextMessageAsync(_config.MasterConversationId, apiRequestException.ToString(), cancellationToken: cancelToken);
        }
    }

    public BotService(IConversationManager conversationManager, IOptions<BotClientConfiguration> config,
        ITaskService taskService, ILogger<BotService> logger)
    {
        _config = config.Value;
        _conversationManager = conversationManager;
        _botClient = new TelegramBotClient(_config.Token);
        _taskService = taskService;
        _logger = logger;
    }

    public async Task Handle(BotTextMessageRequest request, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(request.ConversationId, request.Text, cancellationToken: cancellationToken);
    }

    public async Task Handle(BotGifMessageRequest request, CancellationToken cancellationToken)
    {
        var inputFile = new InputFileUrl(request.GifUrl);
        await _botClient.SendAnimationAsync(request.ConversationId, inputFile, cancellationToken: cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskService.InitializeAsync();
        RunBot(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // loopaloopa
        }
        
        _logger.LogInformation("Stopping Bot");
    }
}

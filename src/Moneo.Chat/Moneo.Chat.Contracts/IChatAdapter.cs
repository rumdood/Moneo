namespace Moneo.Chat;

public interface IChatAdapter
{
    Task StartReceivingAsync(CancellationToken cancellationToken = default);
    Task StartReceivingAsync(string callbackUrl, CancellationToken cancellationToken = default);
    Task StopReceivingAsync(CancellationToken cancellationToken = default);
    Task ReceiveUserMessageAsync(object message, CancellationToken cancellationToken);
    Task SendBotTextMessageAsync(IBotTextMessage botTextMessage, CancellationToken cancellationToken);
    Task SendBotGifMessageAsync(IBotGifMessage botGifMessage, CancellationToken cancellationToken);
    Task<ChatAdapterStatus> GetStatusAsync(CancellationToken cancellationToken);
    bool IsActive { get; }
}

public interface IChatAdapter<in TUserMessage, in TBotTextMessage> : IChatAdapter
    where TUserMessage : class where TBotTextMessage : IBotTextMessage
{
    Task ReceiveMessageAsync(TUserMessage message, CancellationToken cancellationToken);
    Task Handle(TBotTextMessage request, CancellationToken cancellationToken);
}

public record ChatAdapterStatus(string NameOfAdapter, bool IsUsingWebhook, WebhookInfo? WebHookInfo = null);

public record WebhookInfo(string Url, DateTime? LastErrorDate, string? LastErrorMessage, int PendingUpdateCount);

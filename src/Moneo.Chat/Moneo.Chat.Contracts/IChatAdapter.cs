namespace Moneo.Chat;

public interface IChatAdapter
{
    void StartReceiving(CancellationToken cancellationToken = default);
    Task StartReceivingAsync(string callbackUrl, CancellationToken cancellationToken = default);
    Task ReceiveUserMessageAsync(object message, CancellationToken cancellationToken);
    Task SendBotTextMessageAsync(IBotTextMessage botTextMessage, CancellationToken cancellationToken);
    Task SendBotGifMessageAsync(IBotGifMessage botGifMessage, CancellationToken cancellationToken);
}

public interface IChatAdapter<in TUserMessage, in TBotTextMessage> : IChatAdapter
    where TUserMessage : class where TBotTextMessage : IBotTextMessage
{
    Task ReceiveMessageAsync(TUserMessage message, CancellationToken cancellationToken);
    Task Handle(TBotTextMessage request, CancellationToken cancellationToken);
}

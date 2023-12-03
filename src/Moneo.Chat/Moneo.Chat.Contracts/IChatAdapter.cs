namespace Moneo.Chat;

public interface IChatAdapter
{
    void StartReceiving(CancellationToken cancellationToken = default);
    Task StartReceivingAsync(string callbackUrl, CancellationToken cancellationToken = default);
    Task ReceiveMessageAsync(object message, CancellationToken cancellationToken);
}

public interface IChatAdapter<in TUserMessage, in TBotTextMessage> : IChatAdapter
    where TUserMessage : class where TBotTextMessage : IBotTextMessage
{
    Task ReceiveMessageAsync(TUserMessage message, CancellationToken cancellationToken);
    Task Handle(TBotTextMessage request, CancellationToken cancellationToken);
}

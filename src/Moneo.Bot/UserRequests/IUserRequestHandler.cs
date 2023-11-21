namespace Moneo.Chat.UserRequests;

public interface IUserRequestHandler
{
    Task HandleCommand(object command);
}

public interface IUserRequestHandler<in TCommand>: IUserRequestHandler where TCommand : IUserRequest
{
    Task HandleCommand(TCommand command);
}

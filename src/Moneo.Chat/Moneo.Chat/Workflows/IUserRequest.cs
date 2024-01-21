using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat;

public interface IUserRequest : IRequest<MoneoCommandResult>
{
    long ConversationId { get; }
}

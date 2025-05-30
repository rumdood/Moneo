using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat;

public interface IUserRequest : IRequest<MoneoCommandResult>
{
    CommandContext Context { get; }
}

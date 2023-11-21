using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.UserRequests;

public class CompleteTaskRequest : IUserRequest, IRequest<MoneoCommandResult>
{
    public const string CommandKey = "/complete";
    
    public long ConversationId { get; init; }
    public string Name => "Complete";
    public string TaskName { get; init; }

    public CompleteTaskRequest(long conversationId, params string[] args)
    {
        ConversationId = conversationId;
        TaskName = args.Length > 0
            ? string.Join(' ', args)
            : "";
    }
}

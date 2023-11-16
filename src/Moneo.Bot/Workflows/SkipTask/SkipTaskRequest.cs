using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.UserRequests;

public class SkipTaskRequest : IUserRequest, IRequest<MoneoCommandResult>
{
    public const string CommandKey = "/skip";
    
    public long ConversationId { get; init; }
    public string Name => "Skip";
    public string TaskName { get; init; }
    
    public SkipTaskRequest(long conversationId, params string[] args)
    {
        ConversationId = conversationId;
        TaskName = args.Length > 0
            ? string.Join(' ', args)
            : "";
    }
}

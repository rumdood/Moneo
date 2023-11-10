using MediatR;
using Moneo.Bot.Commands;

namespace Moneo.Bot.UserRequests;

public class SkipTaskRequest : IUserRequest, IRequest<MoneoCommandResult>
{
    public const string CommandKey = "/skip";
    
    public long ConversationId { get; init; }
    public string Name => "Skip";
    public string TaskName { get; init; }
    
    public SkipTaskRequest(long conversationId, string taskName)
    {
        ConversationId = conversationId;
        TaskName = taskName;
    }
}

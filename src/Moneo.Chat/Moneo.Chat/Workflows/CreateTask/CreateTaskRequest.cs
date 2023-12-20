using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.UserRequests;

namespace Moneo.Chat.Workflows.CreateTask;

public class CreateTaskRequest : IUserRequest, IRequest<MoneoCommandResult>
{
    public const string CommandKey = "/create";
    
    public long ConversationId { get; init; }
    public string Name => "Create";
    public string TaskName { get; init; }

    public CreateTaskRequest(long conversationId, params string[] args)
    {
        ConversationId = conversationId;
        TaskName = args.Length > 0
            ? string.Join(' ', args)
            : "";
    }
}
using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.UserRequests;

namespace Moneo.Chat.Workflows.CreateTask;

[UserCommand("/create")]
public partial class CreateTaskRequest : UserRequestBase
{
    public string TaskName { get; private set; }

    public CreateTaskRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
}
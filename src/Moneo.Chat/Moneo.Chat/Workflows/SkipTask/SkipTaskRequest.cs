using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.UserRequests;

[UserCommand("/skip")]
public partial class SkipTaskRequest : UserRequestBase
{
    public string TaskName { get; private set; }
    
    public SkipTaskRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
}

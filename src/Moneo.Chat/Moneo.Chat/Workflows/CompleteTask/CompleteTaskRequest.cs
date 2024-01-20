namespace Moneo.Chat;

[UserCommand("/complete")]
public partial class CompleteTaskRequest : UserRequestBase
{
    public string TaskName { get; private set; }

    public CompleteTaskRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }

    public CompleteTaskRequest(long conversationId, string taskName) : base(conversationId, taskName)
    {
        TaskName = taskName;
    }
}

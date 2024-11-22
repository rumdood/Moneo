namespace Moneo.Chat.Workflows.ChangeTask;

[UserCommand(
    CommandKey = "/change",
    HelpDescription = @"Attempts to change a task. Can include a name/description of the task.
example: /change my new task")
]
public partial class ChangeTaskRequest : UserRequestBase
{
    [UserCommandArgument(LongName = nameof(TaskName), HelpText = @"Name of the task to change")]
    public string TaskName { get; private set; }

    public ChangeTaskRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }

    public ChangeTaskRequest(long conversationId, string taskName) : base(conversationId, taskName)
    {
        TaskName = taskName;
    }
}
namespace Moneo.Chat.Workflows.DisableTask;

[UserCommand(
    CommandKey = "/disable",
    HelpDescription = @"Attempts to disable a task. Can include a name/description of the task.
example: /disable my new task")
]
public partial class DisableTaskRequest : UserRequestBase
{
    [UserCommandArgument(LongName = nameof(TaskName), HelpText = @"Name of the task to disable")]
    public string TaskName { get; private set; }

    public DisableTaskRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }

    public DisableTaskRequest(long conversationId, string taskName) : base(conversationId, taskName)
    {
        TaskName = taskName;
    }
}

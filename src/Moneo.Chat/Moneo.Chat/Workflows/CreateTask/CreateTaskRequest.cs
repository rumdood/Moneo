namespace Moneo.Chat.Workflows.CreateTask;

[UserCommand(
    CommandKey = "/create",
    HelpDescription = @"Attempts to create a task. Can include a name/description of the task or I can ask you for one once we get started.
example: /create my new task
example: /create")
]
public partial class CreateTaskRequest : UserRequestBase
{
    [UserCommandArgument(LongName = nameof(TaskName), HelpText = @"Optional name of the task to create")]
    public string TaskName { get; private set; }

    public CreateTaskRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        TaskName = args.Length > 0
            ? string.Join(" ", args)
            : "";
    }
}
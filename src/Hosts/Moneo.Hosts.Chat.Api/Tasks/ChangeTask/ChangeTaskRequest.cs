using Moneo.Chat;
using Moneo.Chat.Models;

namespace Moneo.TaskManagement.Workflows.ChangeTask;

[UserCommand(
    CommandKey = "/change",
    HelpDescription = @"Attempts to change a task. Can include a name/description of the task.
example: /change my new task")
]
public partial class ChangeTaskRequest : UserRequestBase
{
    [UserCommandArgument(LongName = nameof(TaskName), HelpText = @"Name of the task to change")]
    public string TaskName { get; private set; }

    public ChangeTaskRequest(CommandContext context) : base(context)
    {
        TaskName = context.Args.Length > 0
            ? string.Join(" ", context.Args)
            : "";
    }

    public ChangeTaskRequest(long conversationId, ChatUser? user, string taskName) : base(conversationId, user, taskName)
    {
        TaskName = taskName;
    }
}


using Moneo.Chat;
using Moneo.Chat.Models;

namespace Moneo.TaskManagement.Chat;

[UserCommand(
    CommandKey = "/complete", 
    HelpDescription = @"Attempts to complete a task
example: /complete do dishes")]
public partial class CompleteTaskRequest : UserRequestBase
{
    [UserCommandArgument(
        LongName = nameof(TaskName), 
        HelpText = @"The name, description, or id of the task to complete (default argument).
You do not need to label the argument. '/complete my task' will work fine.", 
        IsRequired = true)]
    public string TaskName { get; private set; }

    public CompleteTaskRequest(CommandContext context) : base(context)
    {
        TaskName = context.Args.Length > 0
            ? string.Join(" ", context.Args)
            : "";
    }

    public CompleteTaskRequest(long conversationId, ChatUser? user, string taskName) : base(conversationId, user, taskName)
    {
        TaskName = taskName;
    }
}

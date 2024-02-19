namespace Moneo.Chat;

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

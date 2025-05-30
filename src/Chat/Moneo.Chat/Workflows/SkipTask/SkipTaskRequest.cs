using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;

namespace Moneo.Chat.UserRequests;

[UserCommand(
    CommandKey = "/skip",
    HelpDescription = @"Attempts to skip a task
example: /skip go to the gym")]
public partial class SkipTaskRequest : UserRequestBase
{
    [UserCommandArgument(
        LongName = nameof(TaskName), 
        HelpText = @"The name, description, or id of the task to skip (default argument).
You do not need to label the argument. '/skip my task' will work fine.", 
        IsRequired = true)]
    public string TaskName { get; private set; }

    public SkipTaskRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        TaskName = args.Length > 0
            ? string.Join(' ', args)    
            : "";
    }
}

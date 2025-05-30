using Moneo.Chat.Models;

namespace Moneo.Chat;

[UserCommand(
    CommandKey = "/list",
    HelpDescription = @"Gets a list of active tasks from the server
example: /list")]
public partial class ListTasksRequest : UserRequestBase
{
    public bool AsMenuFlag { get; private set; }

    public ListTasksRequest(CommandContext context) : base(context)
    {
        AsMenuFlag = context.Args.Length > 0 && bool.TryParse(context.Args[0], out var flagValue) && flagValue;
    }

    public ListTasksRequest(long conversationId, ChatUser? user, bool asMenu) : base(conversationId, user, asMenu.ToString())
    {
        AsMenuFlag = asMenu;
    }

    public ListTasksRequest(CommandContext context, bool asMenu) : base(context)
    {
        AsMenuFlag = asMenu;
    }
}


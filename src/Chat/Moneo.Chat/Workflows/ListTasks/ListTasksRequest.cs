using Moneo.Chat.Models;

namespace Moneo.Chat;

[UserCommand(
    CommandKey = "/list",
    HelpDescription = @"Gets a list of active tasks from the server
example: /list")]
public partial class ListTasksRequest : UserRequestBase
{
    public bool AsMenuFlag { get; private set; }

    public ListTasksRequest(long conversationId, ChatUser? user, params string[] args) : base(conversationId, user, args)
    {
        this.AsMenuFlag = args.Length > 0 && bool.TryParse(args[0], out var flagValue) && flagValue;
    }

    public ListTasksRequest(long conversationId, ChatUser? user, bool asMenu) : base(conversationId, user, asMenu.ToString())
    {
        this.AsMenuFlag = asMenu;
    }
}


namespace Moneo.Chat;

[UserCommand("/listTasks")]
public partial class ListTasksRequest : UserRequestBase
{
    public bool AsMenuFlag { get; private set; }

    public ListTasksRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        this.AsMenuFlag = args.Length > 0 && bool.TryParse(args[0], out var flagValue) && flagValue;
    }

    public ListTasksRequest(long conversationId, bool asMenu) : base(conversationId, asMenu.ToString())
    {
        this.AsMenuFlag = asMenu;
    }
}
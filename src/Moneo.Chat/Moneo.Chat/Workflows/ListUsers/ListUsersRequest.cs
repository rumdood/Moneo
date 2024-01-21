namespace Moneo.Chat.Workflows.ListUsers;

[UserCommand("/listUsers")]
public partial class ListUsersRequest : UserRequestBase
{
    public string SearchPattern { get; init; }
    
    public ListUsersRequest(long conversationId, params string[] args) : base(conversationId, args)
    {
        SearchPattern = args.Length > 0 ? args[0] : "*";
    }
}

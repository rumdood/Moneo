namespace Moneo.Chat.Models;

public class ChatUser
{
    public long Id { get; private set; }
    public string FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Username { get; private set; }

    public ChatUser(long id, string firstName, string? lastName = null, string? username = null)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Username = username;
    }
}
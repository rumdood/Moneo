namespace Moneo.Chat.Models;

public record ChatUser(long Id, string FirstName, string? LastName = null, string? Username = null)
{
    public string ReferenceName => Username ?? FirstName;
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("user_conversations")]
public class UserConversation
{
    public long UserId { get; set; }
    public User User { get; set; }

    public long ConversationId { get; set; }
    public Conversation Conversation { get; set; }
}
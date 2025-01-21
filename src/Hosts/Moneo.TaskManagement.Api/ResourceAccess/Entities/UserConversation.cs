using System.ComponentModel.DataAnnotations.Schema;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("user_conversations")]
public class UserConversation
{
    [ForeignKey(nameof(User))]
    [Column("user_id")]
    public long UserId { get; set; }
    public User User { get; set; }

    [ForeignKey(nameof(Conversation))]
    [Column("conversation_id")]
    public long ConversationId { get; set; }
    public Conversation Conversation { get; set; }
}
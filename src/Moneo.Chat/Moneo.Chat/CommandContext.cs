namespace Moneo.Chat;

public class CommandContext
{
    public long ConversationId { get; init; }
    public string CommandKey { get; set; } = default!;
    public string[] Args { get; set; } = [];
    public ChatState CurrentState { get; set; }
}
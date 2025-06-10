namespace Moneo.Chat.Commands;

public enum ResultType
{
    Error = 0,
    NeedMoreInfo = 1,
    WorkflowCompleted = 2
}

public enum ResponseType
{
    None,
    Text,
    Animation,
    Media,
    Menu
}

public enum TextFormat
{
    Plain,
    Markdown,
    Html
}

public class MoneoCommandResult
{
    public string? UserMessageText { get; set; }
    public ResultType Type { get; set; }
    public ResponseType ResponseType { get; set; }
    public TextFormat Format { get; set; } = TextFormat.Plain;
    public HashSet<string> MenuOptions { get; set; }
}

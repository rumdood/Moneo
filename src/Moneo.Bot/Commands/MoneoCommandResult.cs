namespace Moneo.Bot.Commands;

public enum ResultType
{
    Error = 0,
    NeedMoreInfo = 1,
    WorkflowCompleted = 2
}

public enum ResponseType
{
    Text,
    Animation,
    Media,
    Menu
}

public class MoneoCommandResult
{
    public string? UserMessageText { get; set; }
    public ResultType Type { get; set; }
    public ResponseType ResponseType { get; set; }
}

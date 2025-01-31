namespace Moneo.Common;

public sealed record TaskFullId(
    string ChatId,
    string TaskId)
{
    private const char SplitCharacter = '_';
        
    public string FullId => $"{ChatId}{SplitCharacter}{TaskId}";

    public static TaskFullId CreateFromFullId(string fullId)
    {
        var splitIndex = fullId.IndexOf(SplitCharacter);

        if (splitIndex < 1)
        {
            throw new FormatException($"[{fullId}] is not in the proper format ChatId_TaskId");
        }

        var chatId = fullId[..splitIndex];
        var taskId = fullId[(splitIndex + 1)..];

        return new TaskFullId(chatId, taskId);
    }
}
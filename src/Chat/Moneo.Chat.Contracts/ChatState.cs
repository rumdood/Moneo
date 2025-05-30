namespace Moneo.Chat;

/*
public enum ChatState
{
    Waiting,
    CreateTask,
    ChangeTask,
    CreateCron,
    CompleteTask,
    SkipTask,
    ConfirmCommand,
}
*/

public sealed class ChatState
{
    private static readonly Dictionary<string, ChatState> ChatStateRegistry = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; }

    private ChatState(string name)
    {
        Name = name;
        ChatStateRegistry[name] = this;
    }

    public static readonly ChatState Waiting = new("Waiting");
    public static readonly ChatState CreateTask = new("CreateTask");
    public static readonly ChatState ChangeTask = new("ChangeTask");
    public static readonly ChatState CreateCron = new("CreateCron");
    public static readonly ChatState CompleteTask = new("CompleteTask");
    public static readonly ChatState SkipTask = new("SkipTask");
    public static readonly ChatState ConfirmCommand = new("ConfirmCommand");

    // Allow external creation
    // Register a new state (throws if duplicate)
    public static ChatState Register(string name)
    {
        if (ChatStateRegistry.ContainsKey(name))
            throw new ArgumentException($"ChatState '{name}' is already registered.");
        return new ChatState(name);
    }

    // Lookup by name (throws if not found)
    public static ChatState FromName(string name)
    {
        if (ChatStateRegistry.TryGetValue(name, out var state))
            return state;
        throw new ArgumentException($"ChatState '{name}' is not registered.");
    }
    
    public static bool operator ==(ChatState? left, ChatState? right)
        => Equals(left, right);

    public static bool operator !=(ChatState? left, ChatState? right)
        => !Equals(left, right);

    public override string ToString() => Name;
    public override bool Equals(object? obj) => obj is ChatState other && Name == other.Name;
    public override int GetHashCode() => Name.GetHashCode();
}

namespace Moneo.Chat;

public interface ICommandStateRegistry
{
    void RegisterCommand(ChatState state, string commandKey);
    void RegisterCommand(string chatStateName, string commandKey);
    string? GetCommandForState(ChatState state);
    ChatState? GetStateForCommand(string commandKey);
    bool HasCommandForState(ChatState state);
}

public class CommandStateRegistry : ICommandStateRegistry
{
    private readonly Dictionary<string, string> _stateToCommand = new();
    private readonly Dictionary<string, string> _commandToState = new(StringComparer.OrdinalIgnoreCase);
    
    public static CommandStateRegistry Instance { get; } = new();

    public void RegisterCommand(ChatState state, string commandKey) => RegisterCommand(state.Name, commandKey);

    public void RegisterCommand(string chatStateName, string commandKey)
    {
        _stateToCommand[chatStateName] = commandKey;
        _commandToState[commandKey] = chatStateName;
    }

    public string? GetCommandForState(ChatState state) => 
        _stateToCommand.GetValueOrDefault(state.Name);

    public ChatState? GetStateForCommand(string commandKey)
    {
        var stateName = _commandToState.GetValueOrDefault(commandKey);
        if (stateName == null)
            return null;
        
        return ChatState.FromName(stateName);
    }

    public bool HasCommandForState(ChatState state) =>
        _stateToCommand.ContainsKey(state.Name);
}

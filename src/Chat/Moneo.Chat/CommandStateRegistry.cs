namespace Moneo.Chat;

public interface ICommandStateRegistry
{
    void RegisterCommand(ChatState state, string commandKey);
    string? GetCommandForState(ChatState state);
    ChatState? GetStateForCommand(string commandKey);
    bool HasCommandForState(ChatState state);
}

internal class CommandStateRegistry : ICommandStateRegistry
{
    private readonly Dictionary<ChatState, string> _stateToCommand = new();
    private readonly Dictionary<string, ChatState> _commandToState = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterCommand(ChatState state, string commandKey)
    {
        _stateToCommand[state] = commandKey;
        _commandToState[commandKey] = state;
    }

    public string? GetCommandForState(ChatState state) => 
        _stateToCommand.GetValueOrDefault(state);
        
    public ChatState? GetStateForCommand(string commandKey) =>
        _commandToState.GetValueOrDefault(commandKey);
        
    public bool HasCommandForState(ChatState state) =>
        _stateToCommand.ContainsKey(state);
}

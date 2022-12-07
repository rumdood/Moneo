namespace Moneo.Functions.Bot;

public class BotContext
{
    public BotState CurrentState { get; set; }

    public BotContext(BotState state)
    {
        TransitionTo(state);
    }

    public void TransitionTo(BotState newState)
    {
        CurrentState = newState;
        CurrentState.SetContext(this);
    }

    public StateAction GetAction(string messageText) => CurrentState?.GetAction(messageText);
}

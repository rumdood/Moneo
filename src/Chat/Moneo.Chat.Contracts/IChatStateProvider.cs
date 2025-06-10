namespace Moneo.Chat;

public abstract class ChatStateProviderBase
{
    private bool _registrationComplete = false;
    
    public void RegisterStates()
    {
        if (_registrationComplete)
            return;
        
        // get all of the static properties of the current type
        var fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var field in fields)
        {
            // check if the property is of type ChatState
            if (field.FieldType == typeof(ChatState))
            {
                // get the ChatState instance
                _ = (ChatState)field.GetValue(null)!;
            }
        }

        _registrationComplete = true;
    }
}

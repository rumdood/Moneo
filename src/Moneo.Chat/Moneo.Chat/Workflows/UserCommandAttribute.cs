namespace Moneo.Chat;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class UserCommandAttribute : Attribute
{
    public string CommandKey { get; init; }

    public UserCommandAttribute(string commandKey)
    {
        CommandKey = commandKey;
    }
}
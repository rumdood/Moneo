namespace Moneo.Chat;

public class UserMessageFormatException : Exception
{
    public UserMessageFormatException() { }

    public UserMessageFormatException(string message) : base(message) { }

    public UserMessageFormatException(string message, Exception inner) : base(message, inner) { }
}
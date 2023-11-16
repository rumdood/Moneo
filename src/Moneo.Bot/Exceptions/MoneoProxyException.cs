namespace Moneo.Bot.Exceptions;

public class MoneoProxyException : Exception
{
    public MoneoProxyException(string message): base(message) { }
    public MoneoProxyException(string message, Exception innerException) : base(message, innerException) { }
}
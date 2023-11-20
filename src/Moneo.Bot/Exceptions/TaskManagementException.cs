namespace Moneo.Bot.Exceptions;

public class TaskManagementException : Exception
{
    public TaskManagementException(string message): base(message) { }
    public TaskManagementException(string message, Exception innerException) : base(message, innerException) { }
}
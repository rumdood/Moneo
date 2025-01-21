namespace Moneo.TaskManagement.Model;

public class JobUpdateResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = "";
    public Exception? Exception { get; private set; }
    
    public static JobUpdateResult Success() => new() { IsSuccess = true };
    public static JobUpdateResult Failed(string message, Exception? exception = null) =>
        new() { IsSuccess = false, Message = message, Exception = exception };
    public static JobUpdateResult Failed(Exception exception) => new()
        { IsSuccess = false, Message = exception.Message, Exception = exception };
    public static JobUpdateResult TaskNotFound() => new() { IsSuccess = false, Message = "Task not found" };
    public static JobUpdateResult TaskNotActive() => new() { IsSuccess = false, Message = "Task is not active" };
}
namespace Moneo.TaskManagement;

public struct Result<T>
{
    public T Value { get; }
    public bool IsSuccess { get; }
    public string Error { get; }
    public Exception? Exception { get; }

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
        Error = string.Empty;
    }

    private Result(string error, Exception? exception = null)
    {
        Value = default!;
        IsSuccess = false;
        Error = error;
        Exception = exception;
    }
    
    private Result(Exception exception)
    {
        Value = default!;
        IsSuccess = false;
        Error = exception.Message;
        Exception = exception;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, Exception? exception = null) => new(error, exception);
    public static Result<T> Failure(Exception exception) => new(exception);
}

public struct Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, string error, Exception? exception)
    {
        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;
    }

    public static Result Success() => new(true, string.Empty, null);
    public static Result Failure(string error, Exception? exception = null) => new(false, error, exception);
    public static Result Failure(Exception exception) => new(false, exception.Message, exception);
}
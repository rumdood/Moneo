namespace Moneo.TelegramChat.Api;

public interface IMoneoResult
{
    bool IsSuccess { get; }
    string? Message { get; }
    Exception? Exception { get; }
}

public interface IMoneoResult<out TData> : IMoneoResult where TData : class
{
    TData? Data { get; }
}

public class MoneoResult : IMoneoResult
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public Exception? Exception { get; init; }
    
    protected MoneoResult(bool isSuccess, string? message, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Exception = exception;
    }
    
    public static IMoneoResult Success() => new MoneoResult(true, string.Empty);
    
    public static IMoneoResult Error(string message, Exception? exception = null) =>
        new MoneoResult(false, message, exception);
    
    public static IMoneoResult Error(Exception exception) => new MoneoResult(false, exception.Message, exception);
}

public class MoneoResult<TData> : IMoneoResult<TData> where TData : class
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public Exception? Exception { get; init; }
    public TData? Data { get; }
    
    protected MoneoResult(bool isSuccess, string? message, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Exception = exception;
    }
    
    protected MoneoResult(bool isSuccess, TData data, string? message, Exception? exception = null)
    {
        Data = data;
        IsSuccess = isSuccess;
        Message = message;
        Exception = exception;
    }
    
    public static IMoneoResult<TData> Success() => new MoneoResult<TData>(true, string.Empty);
    
    public static IMoneoResult<TData> Success(TData data) => new MoneoResult<TData>(true, data, string.Empty);

    public static IMoneoResult<TData> Error(string message, Exception? exception = null) =>
        new MoneoResult<TData>(false, message, exception);

    public static IMoneoResult<TData> Error(TData data, string message, Exception? exception = null) =>
        new MoneoResult<TData>(false, data, message, exception);
    
    public static IMoneoResult<TData> Error(Exception exception) => new MoneoResult<TData>(false, exception.Message, exception);

    public static IMoneoResult<TData> Error(TData data, Exception exception) =>
        new MoneoResult<TData>(false, data, exception.Message, exception);
}

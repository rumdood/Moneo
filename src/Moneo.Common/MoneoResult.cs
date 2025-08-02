using Ardalis.SmartEnum;

namespace Moneo.Common;

public interface IMoneoResult
{
    MoneoResultType Type { get; }
    string Message { get; }
    Exception? Exception { get; }
}

public interface IMoneoResult<out TData> : IMoneoResult
{
    TData? Data { get; }
}

public class MoneoResult<TData> : MoneoResult, IMoneoResult<TData>
{
    public TData? Data { get; set; }

    public static MoneoResult<TData> Success(TData data) => new()
        { Data = data, Type = MoneoResultType.Success };
    public static MoneoResult<TData> Success(TData data, string message) => new()
        { Data = data, Type = MoneoResultType.Success, Message = message };
    public static MoneoResult<TData> Created(TData data) => new()
        { Data = data, Type = MoneoResultType.Created };
    public static MoneoResult<TData> Created(TData data, string message) => new()
        { Data = data, Type = MoneoResultType.Created, Message = message };
    public static MoneoResult<TData> NoChange(TData data) => new()
        { Data = data, Type = MoneoResultType.NoChange };
    public static MoneoResult<TData> NoChange(TData data, string message) => new()
        { Data = data, Type = MoneoResultType.NoChange, Message = message };
    public static MoneoResult<TData> Failed(string message, Exception? exception = null) => new()
        { Type = MoneoResultType.Failed, Message = message, Exception = exception };
    public static MoneoResult<TData> Failed(Exception exception) => new()
        { Type = MoneoResultType.Failed, Message = exception.Message, Exception = exception };
    public static MoneoResult<TData> TaskNotFound(string message) => new()
        { Type = MoneoResultType.TaskNotFound, Message = message };
    public static MoneoResult<TData> AlreadyExists(string message) => new()
        { Type = MoneoResultType.TaskAlreadyExists, Message = message };
    public static MoneoResult<TData> ConversationNotFound(string message) => new()
        { Type = MoneoResultType.ConversationNotFound, Message = message };
    public static MoneoResult<TData> BadRequest(string message) => new()
        { Type = MoneoResultType.BadRequest, Message = message };
    public static MoneoResult<TData> NotFound(string message) => new()
        { Type = MoneoResultType.NotFound, Message = message };
}

public class MoneoResult : IMoneoResult
{
    public MoneoResultType Type { get; set; } = MoneoResultType.None;
    public bool IsSuccess => (Type & MoneoResultType.Success) == MoneoResultType.Success;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    
    public static MoneoResult Success() => new() { Type = MoneoResultType.Success };
    public static MoneoResult Success(string message) => new() { Type = MoneoResultType.Success, Message = message };
    public static MoneoResult Created() => new() { Type = MoneoResultType.Created };
    public static MoneoResult Created(string message) => new() { Type = MoneoResultType.Created, Message = message };
    public static MoneoResult NoChange() => new() { Type = MoneoResultType.NoChange };
    public static MoneoResult NoChange(string message) => new() { Type = MoneoResultType.NoChange, Message = message };
    public new static MoneoResult Failed(string message, Exception? exception = null) => new()
        { Type = MoneoResultType.Failed, Message = message, Exception = exception };
    public new static MoneoResult Failed(Exception exception) => new()
        { Type = MoneoResultType.Failed, Message = exception.Message, Exception = exception };
    public new static MoneoResult TaskNotFound(string message) => new()
        { Type = MoneoResultType.TaskNotFound, Message = message };
    public new static MoneoResult AlreadyExists(string message) => new()
        { Type = MoneoResultType.TaskAlreadyExists, Message = message };
    public new static MoneoResult ConversationNotFound(string message) => new()
        { Type = MoneoResultType.ConversationNotFound, Message = message };
    public new static MoneoResult BadRequest(string message) => new()
        { Type = MoneoResultType.BadRequest, Message = message };
    public new static MoneoResult NotFound(string message) => new()
        { Type = MoneoResultType.NotFound, Message = message };
}

public sealed class MoneoResultType : SmartFlagEnum<MoneoResultType>
{
    private const int SuccessBit = 1 << 0;
    
    public static readonly MoneoResultType None = new(nameof(None), 0);
    public static readonly MoneoResultType Success = new(nameof(Success), SuccessBit);
    public static readonly MoneoResultType NoChange = new(nameof(NoChange), SuccessBit | 1 << 1);
    public static readonly MoneoResultType Failed = new(nameof(Failed), 1 << 2);
    public static readonly MoneoResultType TaskNotFound = new(nameof(TaskNotFound), 1 << 3);
    public static readonly MoneoResultType TaskAlreadyExists = new(nameof(TaskAlreadyExists), 1 << 4);
    public static readonly MoneoResultType ConversationNotFound = new(nameof(ConversationNotFound), 1 << 5);
    public static readonly MoneoResultType BadRequest = new(nameof(BadRequest), 1 << 6);
    public static readonly MoneoResultType Created = new(nameof(Created), SuccessBit | 1 << 7);
    public static readonly MoneoResultType NotFound = new(nameof(NotFound), 1 << 8);
    
    public bool IsSuccess => (Value & SuccessBit) == SuccessBit;

    private MoneoResultType(string name, int value) : base(name, value)
    {
    }
}

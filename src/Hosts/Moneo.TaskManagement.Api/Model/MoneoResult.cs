using Ardalis.SmartEnum;

namespace Moneo.TaskManagement.Model;

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

public class MoneoResult<TData> : IMoneoResult<TData>
{
    public TData? Data { get; set; }
    public MoneoResultType Type { get; set; } = MoneoResultType.None;
    public bool IsSuccess => (Type & MoneoResultType.Success) == MoneoResultType.Success;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

    public static MoneoResult<TData> Success(TData data) => new()
        { Data = data, Type = MoneoResultType.Success };
    public static MoneoResult<TData> NoChange(TData data) => new()
        { Data = data, Type = MoneoResultType.NoChange };
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
}

public class MoneoResult : MoneoResult<object>
{
    public new static MoneoResult Success() => new() { Type = MoneoResultType.Success };
    public new static MoneoResult NoChange() => new() { Type = MoneoResultType.NoChange };
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
    
    public bool IsSuccess => (Value & SuccessBit) == SuccessBit;

    private MoneoResultType(string name, int value) : base(name, value)
    {
    }
}

public static class MoneoResultToHttpResultMapper
{
    private static readonly Dictionary<MoneoResultType, Func<MoneoResult, IResult>> ResultMap = new()
    {
        { MoneoResultType.Success, r => Results.Ok(r.Data ?? r.Message) },
        {
            MoneoResultType.Failed,
            r => Results.Problem(title: "Internal Server Error",
                detail: string.IsNullOrEmpty(r.Message) ? r.Exception?.Message : r.Message, statusCode: 500)
        },
        { MoneoResultType.TaskNotFound, r => Results.NotFound(r.Message) },
        { MoneoResultType.TaskAlreadyExists, r => Results.Conflict(r.Message) },
        { MoneoResultType.ConversationNotFound, r => Results.NotFound(r.Message) }
    };
    
    public static IResult GetHttpResult<TData>(this MoneoResult<TData> result)
    {
        var r = result as MoneoResult;
        return ResultMap.TryGetValue(result.Type, out var resultFunc) ? resultFunc(r) : Results.NoContent();
    }
}

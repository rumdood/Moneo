using Moneo.Common;

namespace Moneo.TaskManagement;

public static class MoneoResultToHttpResultMapper
{
    private static object? GetData(IMoneoResult result)
    {
        // if the result is a MoneoResult<TData> then return the data as an object
        return result.GetType().GetProperty("Data")?.GetValue(result);
    }
    
    private static readonly Dictionary<MoneoResultType, Func<IMoneoResult, IResult>> ResultMap = new()
    {
        { MoneoResultType.Success, r => Results.Ok(GetData(r) ?? r.Message) },
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
        return ResultMap.TryGetValue(result.Type, out var resultFunc) ? resultFunc(result) : Results.NoContent();
    }
}

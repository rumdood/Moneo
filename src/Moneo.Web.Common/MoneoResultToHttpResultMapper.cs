using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Moneo.Common;

namespace Moneo.Web;

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
        // TODO: Figure out the URI for created resources
        { MoneoResultType.Created, r => Results.Created((Uri?)null, GetData(r) ?? r.Message) },
        {
            MoneoResultType.Failed,
            r => Results.Problem(title: "Internal Server Error",
                detail: string.IsNullOrEmpty(r.Message) ? r.Exception?.Message : r.Message, statusCode: 500)
        },
        { MoneoResultType.TaskNotFound, r => Results.NotFound(r.Message) },
        { MoneoResultType.ConversationNotFound, r => Results.NotFound(r.Message) },
        { MoneoResultType.TaskAlreadyExists, r => Results.Conflict(r.Message) },
    };
    
    /*
    public static IResult GetHttpResult<TData>(this MoneoResult<TData> result)
    {
        return ResultMap.TryGetValue(result.Type, out var resultFunc) ? resultFunc(result) : Results.NoContent();
    }
    */

    public static IResult GetHttpResult(this MoneoResult result)
    {
        return ResultMap.TryGetValue(result.Type, out var resultFunc) ? resultFunc(result) : Results.NoContent();
    }
}

public static class HttpResponseMessageExtensions
{
    public static async Task<MoneoResult<TData>> GetMoneoResultAsync<TData>(
        this HttpResponseMessage responseMessage, 
        CancellationToken cancellationToken = default)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            var errorContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            return MoneoResult<TData>.Failed(
                responseMessage.ReasonPhrase ?? "Request Failed",
                new InvalidOperationException(errorContent));
        }

        if (responseMessage.Content.Headers.ContentType?.MediaType != "application/json")
        {
            return MoneoResult<TData>.Failed("Response was not in the expected format.");
        }
        
        var s = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        
        var data = await responseMessage.Content.ReadFromJsonAsync<TData>(cancellationToken: cancellationToken);
        return data is not null
            ? MoneoResult<TData>.Success(data)
            : MoneoResult<TData>.Failed("Failed to deserialize response.");
    }
    
    public static async Task<MoneoResult> GetMoneoResultAsync(
        this HttpResponseMessage responseMessage, 
        CancellationToken cancellationToken = default)
    {
        var message = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        
        if (!responseMessage.IsSuccessStatusCode)
        {
            return MoneoResult.Failed(
                responseMessage.ReasonPhrase ?? "Request Failed",
                new InvalidOperationException(message));
        }
        
        return MoneoResult.Success(message);
    }
}

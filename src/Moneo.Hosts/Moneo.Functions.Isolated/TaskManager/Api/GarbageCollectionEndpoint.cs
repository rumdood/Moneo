using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class GarbageCollectionEndpoint : TaskManagerEndpointBase
{
    private static readonly HttpClient client = new HttpClient();
    private const string entityName = "taskmanager";

    public GarbageCollectionEndpoint(IDurableEntityTasksService tasksService, ILogger<TaskManagerEndpointBase> log) : base(tasksService, log)
    {
    }

    private static async Task<(string ContinuationToken, List<string> Keys)> GetEntities(string baseUrl, string entityName, string continuationToken)
    {
        var listEntitiesUri = $"{baseUrl}/runtime/webhooks/durabletask/entities/{entityName}";
        Console.WriteLine(listEntitiesUri);

        client.DefaultRequestHeaders.Add("x-ms-continuation-token", continuationToken);
        var response = await client.GetAsync(listEntitiesUri);
        var body = JArray.Parse(await response.Content.ReadAsStringAsync());

        if (response.Headers.TryGetValues("x-ms-continuation-token", out var values))
        {
            continuationToken = values.FirstOrDefault();
        }

        var keys = body.SelectTokens("$..entityId.key").Values<string>().Where(key => !string.IsNullOrEmpty(key)).ToList();

        return (continuationToken, keys);
    }

    [Function("GarbageCollection")]
    public static async Task<HttpResponseData> PerformGarbageCollection(
        [HttpTrigger(AuthorizationLevel.Admin, HttpVerbs.Post, Route = "tasks/garbage")]
        HttpRequestData request,
        FunctionContext context)
    {
        var baseUrl = $"{request.Url.Scheme}://{request.Url.Host}";
        var code = MoneoConfiguration.DurableTaskFunctionKey;
        client.DefaultRequestHeaders.Add("x-functions-key", code);

        var result = await GetEntities(baseUrl, entityName, "");
        var keys = result.Keys;

        while (!string.IsNullOrEmpty(result.ContinuationToken))
        {
            result = await GetEntities(baseUrl, entityName, result.ContinuationToken);
            keys.AddRange(result.Keys);
        }

        foreach (var key in keys)
        {
            var purgeSingleInstanceHistoryUri = $"{baseUrl}/runtime/webhooks/durabletask/instances/@{entityName}@{key}";
            await client.DeleteAsync(purgeSingleInstanceHistoryUri);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Purged {keys.Count} entities");
        return response;
    }
}

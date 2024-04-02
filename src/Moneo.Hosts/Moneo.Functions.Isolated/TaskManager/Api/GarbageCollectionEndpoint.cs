using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Moneo.Functions.Isolated.TaskManager;

internal class GarbageCollectionEndpoint : TaskManagerEndpointBase
{
    private static readonly HttpClient Client = new();
    private const string EntityName = "taskmanager";

    public GarbageCollectionEndpoint(IDurableEntityTasksService tasksService, ILogger<GarbageCollectionEndpoint> log) : base(tasksService, log)
    {
    }

    private static async Task<(string ContinuationToken, List<string> Keys)> GetEntities(string baseUrl, string entityName, string continuationToken)
    {
        var listEntitiesUri = $"{baseUrl}/runtime/webhooks/durabletask/entities/{entityName}";
        Console.WriteLine(listEntitiesUri);

        Client.DefaultRequestHeaders.Add("x-ms-continuation-token", continuationToken);
        var response = await Client.GetAsync(listEntitiesUri);
        var body = JArray.Parse(await response.Content.ReadAsStringAsync());

        if (response.Headers.TryGetValues("x-ms-continuation-token", out var values))
        {
            continuationToken = values.FirstOrDefault() ?? "";
        }

        var keys = body?.SelectTokens("$..entityId.key").Values<string>().Where(key => !string.IsNullOrEmpty(key))
            .Select(k => k!)
            .ToList() ?? [];

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
        Client.DefaultRequestHeaders.Add("x-functions-key", code);

        var result = await GetEntities(baseUrl, EntityName, "");
        var keys = result.Keys;

        while (!string.IsNullOrEmpty(result.ContinuationToken))
        {
            result = await GetEntities(baseUrl, EntityName, result.ContinuationToken);
            keys.AddRange(result.Keys);
        }

        foreach (var key in keys)
        {
            var purgeSingleInstanceHistoryUri = $"{baseUrl}/runtime/webhooks/durabletask/instances/@{EntityName}@{key}";
            await Client.DeleteAsync(purgeSingleInstanceHistoryUri);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Purged {keys.Count} entities");
        return response;
    }
}

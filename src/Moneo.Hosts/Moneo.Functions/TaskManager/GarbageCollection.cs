using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Moneo.Functions;

public class GarbageCollection
{
    private static readonly HttpClient client = new HttpClient();
    private const string entityName = "taskmanager";

    [FunctionName("GarbageCollection")]
    public static async Task<IActionResult> PerformGarbageCollection(
        [HttpTrigger(AuthorizationLevel.Admin, HttpVerbs.Post, Route = "tasks/garbage")]
        HttpRequest request
        )
    {
        var baseUrl = $"https://{request.Host.Value}";
        var code = Environment.GetEnvironmentVariable("DurableTaskFunctionKey");
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

        return new OkObjectResult($"Purged {keys.Count} entities");
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
}

using Microsoft.Extensions.Logging;
using Moneo.TaskManagement.Models;

namespace Moneo.Functions.Isolated.TaskManager;

internal static class HttpVerbs
{
    public const string Delete = "delete";
    public const string Get = "get";
    public const string Patch = "patch";
    public const string Post = "post";
    public const string Put = "put";
}

internal record TaskFunctionResult(bool Success, string? Message = null);

internal record DurableEntityEntry(string Key, MoneoTaskDto TaskDto);

internal abstract class TaskManagerEndpointBase
{
    protected readonly IDurableEntityTasksService _durableEntityTasksService;
    protected readonly ILogger<TaskManagerEndpointBase> _logger;

    protected TaskManagerEndpointBase(IDurableEntityTasksService tasksService, ILogger<TaskManagerEndpointBase> log)
    {
        _durableEntityTasksService = tasksService;
        _logger = log;
    }
}

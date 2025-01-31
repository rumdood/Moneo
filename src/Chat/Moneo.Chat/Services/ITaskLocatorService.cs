using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moneo.Common;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.Chat.Services;

public interface ITaskLocatorService
{
    Task<MoneoResult<PagedList<MoneoTaskDto>>> LocateTaskByKeywordsAsync(
        string keywords,
        PageOptions pagingOptions,
        CancellationToken cancellationToken = default);
}

internal class TaskLocatorService : ITaskLocatorService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITaskManagerClient _taskManagerClient;
    private readonly ILogger<TaskLocatorService> _logger;

    public TaskLocatorService(ITaskManagerClient taskManagerClient)
    {
        _taskManagerClient = taskManagerClient;
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> LocateTaskByKeywordsAsync(
        string keywords,
        PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        var result = await _taskManagerClient.GetTasksForConversationAsync(0, pagingOptions, cancellationToken);
        return result;
    }
}

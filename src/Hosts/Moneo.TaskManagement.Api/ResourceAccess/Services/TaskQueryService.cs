using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.ResourceAccess;

internal class TaskQueryService : ITaskQueryService
{
    private readonly MoneoTasksDbContext _dbContext;

    public TaskQueryService(MoneoTasksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MoneoTaskDto>> GetTasksForUserAsync(long userId)
    {
        var tasks = await _dbContext.UserConversations
            .AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .SelectMany(uc => uc.Conversation.Tasks)
            .Select(task => task.ToDto())
            .ToListAsync();

        return tasks;
    }

    public async Task<IReadOnlyList<MoneoTaskDto>> GetTasksForConversationAsync(long conversationId)
    {
        var tasks = await _dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .SelectMany(c => c.Tasks)
            .Select(task => task.ToDto())
            .ToListAsync();

        return tasks;
    }

    public async Task<IReadOnlyList<MoneoTaskDto>> GetTasksForUserAndConversationAsync(long userId, long conversationId)
    {
        var tasks = await _dbContext.UserConversations
            .AsNoTracking()
            .Where(uc => uc.UserId == userId && uc.ConversationId == conversationId)
            .SelectMany(uc => uc.Conversation.Tasks)
            .Select(task => task.ToDto())
            .ToListAsync();

        return tasks;
    }

    public async Task<MoneoTaskDto?> GetTaskAsync(long taskId)
    {
        var task = await _dbContext.MoneoTasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .FirstOrDefaultAsync();

        return task?.ToDto();
    }

    public Task<MoneoTaskWithHistoryDto?> GetTaskWithHistoryAsync(long taskId, int maxHistoryRecords = 10)
        => GetTaskWithHistoryAsync(TaskFilter.ForTask(taskId), maxHistoryRecords);

    public async Task<MoneoTaskDto?> GetTaskAsync(TaskFilter filter)
    {
        var task = await _dbContext.MoneoTasks
            .AsNoTracking()
            .Where(filter.ToExpression())
            .Include(moneoTask => moneoTask.TaskRepeater)
            .FirstOrDefaultAsync();

        return task?.ToDto();
    }

    public async Task<MoneoTaskWithHistoryDto?> GetTaskWithHistoryAsync(TaskFilter filter, int maxHistoryRecords = 10)
    {
        var query = _dbContext.MoneoTasks
            .AsNoTracking()
            .Where(filter.ToExpression())
            .Include(moneoTask => moneoTask.TaskRepeater)
            .Select(moneoTask => new
            {
                Task = moneoTask,
                History = moneoTask.TaskEvents
                    .OrderByDescending(h => h.Timestamp)
                    .Take(maxHistoryRecords)
                    .Select(h => h.ToDto())
                    .ToList()
            });

        var result = await query.SingleOrDefaultAsync();

        return result?.Task.ToDtoWithHistory(result.History);
    }

    public async Task<IReadOnlyList<MoneoTaskDto>> GetTasksAsync(TaskFilter filter)
    {
        var tasks = await _dbContext.MoneoTasks
            .AsNoTracking()
            .Where(filter.ToExpression())
            .Include(moneoTask => moneoTask.TaskRepeater)
            .Select(t => t.ToDto())
            .ToListAsync();

        return tasks;
    }
}

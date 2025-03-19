using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.Jobs;

internal record MoneoTaskCompletionDataDto(
    long Id,
    string Name,
    long ConversationId,
    bool IsActive,
    TaskBadgerDto? Badger,
    TaskRepeaterDto? Repeater,
    DateTime? LastCompletedOrSkipped);

internal static class TaskDbSetExtensions
{
    public static async Task<MoneoTaskCompletionDataDto?> GetTaskWithHistoryDataAsync(
        this DbSet<MoneoTask> tasks,
        long taskId,
        bool includeBadger = false,
        CancellationToken cancellationToken = default)
    {
        var task = await tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new MoneoTaskCompletionDataDto(
                t.Id,
                t.Name,
                t.ConversationId,
                t.IsActive,
                includeBadger ? t.Badger != null ? t.Badger.ToDto() : null : null,
                t.Repeater != null ? t.Repeater.ToDto() : null,
                t.TaskEvents
                    .Where(h => h.Type == TaskEventType.Completed || h.Type == TaskEventType.Skipped)
                    .OrderByDescending(h => h.OccurredOn)
                    .Select(h => (DateTime?)h.OccurredOn)
                    .FirstOrDefault()
            ))
            .SingleOrDefaultAsync(cancellationToken);

        return task;
    }
}
namespace Moneo.TaskManagement.ResourceAccess;

using Moneo.TaskManagement.Model;

internal interface ITaskQueryService
{
    Task<IReadOnlyList<MoneoTaskDto>> GetTasksForUserAsync(long userId);
    Task<IReadOnlyList<MoneoTaskDto>> GetTasksForConversationAsync(long conversationId);

    Task<IReadOnlyList<MoneoTaskDto>> GetTasksForUserAndConversationAsync(long userId, long conversationId);
    Task<MoneoTaskDto?> GetTaskAsync(long taskId);
    Task<MoneoTaskWithHistoryDto?> GetTaskWithHistoryAsync(long taskId, int maxHistoryRecords = 10);
    Task<MoneoTaskDto?> GetTaskAsync(TaskFilter filter);
    Task<MoneoTaskWithHistoryDto?> GetTaskWithHistoryAsync(TaskFilter filter, int maxHistoryRecords = 10);
    Task<IReadOnlyList<MoneoTaskDto>> GetTasksAsync(TaskFilter filter);
}
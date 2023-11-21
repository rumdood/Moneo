using Moneo.Models.TaskManagement;

namespace Moneo.TaskManagement;

public interface ITaskResourceManager
{
    Task InitializeAsync();
    Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetAllTasksForUserAsync(long conversationId);
    Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskId);
}
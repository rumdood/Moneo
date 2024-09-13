using Moneo.TaskManagement.Client.Models;
using Moneo.TaskManagement.Models;

namespace Moneo.TaskManagement;

public interface ITaskResourceManager
{
    Task InitializeAsync();
    Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetAllTasksForUserAsync(long conversationId);
    Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult> CreateTaskAsync(long conversationId, MoneoTaskDto task);
    Task<MoneoTaskResult> DisableTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetTasksForUserAsync(long conversationId, MoneoTaskFilter filter);
}
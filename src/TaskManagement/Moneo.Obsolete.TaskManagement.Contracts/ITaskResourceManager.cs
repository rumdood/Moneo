using Moneo.Obsolete.TaskManagement.Client.Models;
using Moneo.Obsolete.TaskManagement.Models;

namespace Moneo.Obsolete.TaskManagement;

public interface ITaskResourceManager
{
    Task InitializeAsync();
    Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetAllTasksForUserAsync(long conversationId);
    Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult> CreateTaskAsync(long conversationId, MoneoTaskDto task);
    Task<MoneoTaskResult> DisableTaskAsync(long conversationId, string taskId);
    Task<MoneoTaskResult> UpdateTaskAsync(long conversationId, string taskId, MoneoTaskDto task);
    Task<MoneoTaskResult<IEnumerable<MoneoTaskDto>>> GetTasksForUserAsync(long conversationId, MoneoTaskFilter filter);
}
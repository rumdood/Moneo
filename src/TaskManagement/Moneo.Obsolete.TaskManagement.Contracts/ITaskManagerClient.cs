using Moneo.Obsolete.TaskManagement.Client.Models;
using Moneo.Obsolete.TaskManagement.Models;

namespace Moneo.Obsolete.TaskManagement;

public interface ITaskManagerClient
{
    Task<MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>> GetAllTasksAsync();
    Task<MoneoTaskResult<Dictionary<string, MoneoTaskDto>>> GetTasksForConversation(long conversationId);
    Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskName);
    Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskName);
    Task<MoneoTaskResult> CreateTaskAsync(long conversationId, MoneoTaskDto task);
    Task<MoneoTaskResult> DisableTaskAsync(long conversationId, string taskName);
    Task<MoneoTaskResult> UpdateTaskAsync(long conversationId, string taskName, MoneoTaskDto task);
}
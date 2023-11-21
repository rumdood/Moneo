using Moneo.Models.TaskManagement;

namespace Moneo.TaskManagement;

public interface ITaskManagerClient
{
    Task<MoneoTaskResult<Dictionary<string, MoneoTaskManagerDto>>> GetAllTasksAsync();
    Task<MoneoTaskResult<Dictionary<string, MoneoTaskDto>>> GetTasksForConversation(long conversationId);
    Task<MoneoTaskResult> CompleteTaskAsync(long conversationId, string taskName);
    Task<MoneoTaskResult> SkipTaskAsync(long conversationId, string taskName);
}
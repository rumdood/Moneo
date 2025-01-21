using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.TaskManagement.Contracts;

public interface ITaskManagerClient
{
    Task<IReadOnlyList<MoneoTaskDto>> GetTasksForConversationAsync(long conversationId);
    Task<IReadOnlyList<MoneoTaskDto>> GetTasksForUserAsync(long userId);
    Task<IReadOnlyList<MoneoTaskDto>> GetTasksForUserAndConversationAsync(long userId, long conversationId);
    Task<MoneoTaskDto?> GetTaskAsync(long taskId);
    Task<MoneoTaskDto> CreateTaskAsync(CreateEditTaskDto dto);
    Task UpdateTaskAsync(long taskId, CreateEditTaskDto dto);
    Task DeleteTaskAsync(long taskId);
    Task CompleteTaskAsync(long taskId);
    Task SkipTaskAsync(long taskId);
    Task DeactivateTaskAsync(long taskId);
}
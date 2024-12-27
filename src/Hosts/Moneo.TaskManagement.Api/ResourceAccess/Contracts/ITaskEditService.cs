namespace Moneo.TaskManagement.ResourceAccess;

using Moneo.TaskManagement.Model;

public interface ITaskEditService
{
    Task<MoneoTaskDto> CreateTaskForConversationAsync(long conversationId, CreateTaskDto taskDto);
    Task<MoneoTaskDto> UpdateTaskAsync(long taskId, UpdateTaskDto taskDto);
    Task<Result<MoneoTaskDto>> TryUpdateTaskAsync(TaskFilter filter, UpdateTaskDto taskDto);
    Task<Result> DeleteTaskAsync(long taskId);
    Task<Result> TryDeleteTaskAsync(TaskFilter filter);
    Task CompleteTaskAsync(long taskId);
    Task<Result> TryCompleteTaskAsync(TaskFilter filter);
    Task SkipTaskAsync(long taskId);
    Task<Result> TrySkipTaskAsync(TaskFilter filter);
    Task DeactivateTaskAsync(long taskId);
    Task<Result> TryDeactivateTaskAsync(TaskFilter filter);
}

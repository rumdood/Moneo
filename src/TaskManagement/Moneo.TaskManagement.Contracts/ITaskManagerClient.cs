
using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.TaskManagement.Contracts;

public interface ITaskManagerClient
{
    Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForConversationAsync(
        long conversationId, 
        PageOptions pagingOptions,
        CancellationToken cancellationToken = default);

    Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAsync(
        long userId,
        PageOptions pagingOptions,
        CancellationToken cancellationToken = default);

    Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAndConversationAsync(
        long userId, 
        long conversationId,
        PageOptions pagingOptions, 
        CancellationToken cancellationToken = default);
    
    Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksByKeywordSearchAsync(
        long conversationId,
        string keyword,
        PageOptions pagingOptions,
        CancellationToken cancellationToken = default);
    Task<MoneoResult<MoneoTaskDto>> GetTaskAsync(long taskId, CancellationToken cancellationToken = default);

    Task<MoneoResult<MoneoTaskDto>> CreateTaskAsync(
        long conversationId, 
        CreateEditTaskDto dto,
        CancellationToken cancellationToken = default);

    Task<MoneoResult> UpdateTaskAsync(
        long taskId, 
        CreateEditTaskDto dto,
        CancellationToken cancellationToken = default);
    Task<MoneoResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default);
    Task<MoneoResult> CompleteTaskAsync(long taskId, CancellationToken cancellationToken = default);
    Task<MoneoResult> SkipTaskAsync(long taskId, CancellationToken cancellationToken = default);
    Task<MoneoResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken = default);
}

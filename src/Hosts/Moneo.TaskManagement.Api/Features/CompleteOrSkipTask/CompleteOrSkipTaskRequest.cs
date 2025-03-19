using MediatR;
using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.Api.Features.CompleteTask;

public enum TaskCompletionType
{
    None,
    Completed,
    Skipped
}

public sealed record CompleteOrSkipTaskRequest(long TaskId, TaskCompletionType Type) : IRequest<MoneoResult>;

internal class CompleteTaskRequestHandler : IRequestHandler<CompleteOrSkipTaskRequest, MoneoResult>
{
    private readonly MoneoTasksDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    
    public CompleteTaskRequestHandler(MoneoTasksDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }
    
    public async Task<MoneoResult> Handle(CompleteOrSkipTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var task = await _dbContext.Tasks.FindAsync([request.TaskId], cancellationToken: cancellationToken);
            if (task is null)
            {
                return MoneoResult.TaskNotFound("Task not found");
            }
        
            if (task.IsActive == false)
            {
                return MoneoResult.BadRequest("Task is inactive");
            }

            if (request.Type == TaskCompletionType.None)
            {
                return MoneoResult.BadRequest("Unknown completion type (must be Completed or Skipped)");
            }
            
            if (request.Type == TaskCompletionType.Skipped && task.CanBeSkipped == false)
            {
                return MoneoResult.BadRequest("Task cannot be skipped");
            }
            
            var eventType = request.Type switch
            {
                TaskCompletionType.Completed => TaskEventType.Completed,
                TaskCompletionType.Skipped => TaskEventType.Skipped,
                _ => throw new ArgumentOutOfRangeException()
            };
        
            // add a task event to log the completion
            var taskEvent = new TaskEvent(task, eventType, _timeProvider.GetUtcNow().UtcDateTime);
            await _dbContext.TaskEvents.AddAsync(taskEvent, cancellationToken);

            if (task.Repeater is null)
            {
                task.IsActive = false;
            }

            switch (eventType)
            {
                case TaskEventType.Completed:
                    task.DomainEvents.Add(new TaskDomainCompleted(_timeProvider.GetUtcNow().UtcDateTime, task));
                    break;
                case TaskEventType.Skipped:
                    task.DomainEvents.Add(new TaskSkipped(_timeProvider.GetUtcNow().UtcDateTime, task));
                    break;
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}

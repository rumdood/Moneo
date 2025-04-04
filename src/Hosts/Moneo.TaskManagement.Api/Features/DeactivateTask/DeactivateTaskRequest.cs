using MediatR;
using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess;
using TaskEvent = Moneo.TaskManagement.ResourceAccess.Entities.TaskEvent;

namespace Moneo.TaskManagement.Features.DeactivateTask;

public sealed record DeactivateTaskRequest(long TaskId) : IRequest<MoneoResult>;

internal sealed class DeactivateTaskRequestHandler(MoneoTasksDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<DeactivateTaskRequest, MoneoResult>
{
    public async Task<MoneoResult> Handle(DeactivateTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var task = await dbContext.Tasks.FindAsync([request.TaskId], cancellationToken: cancellationToken);
            if (task is null)
            {
                return MoneoResult.TaskNotFound("Task not found");
            }
        
            if (task.IsActive == false)
            {
                return MoneoResult.BadRequest("Task is already inactive");
            }
        
            // add a task event to log the deactivation
            var taskEvent = new TaskEvent(task, TaskEventType.Disabled, timeProvider.GetUtcNow().UtcDateTime);
            await dbContext.TaskEvents.AddAsync(taskEvent, cancellationToken);

            task.IsActive = false;
            task.DomainEvents.Add(new TaskDeactivated(timeProvider.GetUtcNow().UtcDateTime, task));
            
            await dbContext.SaveChangesAsync(cancellationToken);
            return MoneoResult.Success();
        }
        catch (Exception e)
        {
            return MoneoResult.Failed(e);
        }
    }
}

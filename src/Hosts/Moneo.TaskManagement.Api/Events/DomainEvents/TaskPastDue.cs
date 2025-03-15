using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moneo.TaskManagement.Scheduling;
using Quartz;

namespace Moneo.TaskManagement.DomainEvents;

public sealed record TaskPastDue(DateTime OccuredOn, long TaskId) : DomainEvent(OccuredOn);

internal class TaskPastDueEventHandler : INotificationHandler<TaskPastDue>
{
    private readonly MoneoTasksDbContext _dbContext;
    private readonly ILogger<TaskPastDueEventHandler> _logger;
    private readonly ISchedulerService _schedulerService;

    public TaskPastDueEventHandler(
        ILogger<TaskPastDueEventHandler> logger,
        ISchedulerService schedulerService,
        MoneoTasksDbContext dbContext)
    {
        _logger = logger;
        _schedulerService = schedulerService;
        _dbContext = dbContext;
    }
    
    public async Task Handle(TaskPastDue notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling TaskPastDue event for task {TaskId}", notification.TaskId);
        
        var scheduler = _schedulerService.GetScheduler();

        if (scheduler is null)
        {
            _logger.LogError("Scheduler is not available");
            return;
        }

        var task = await _dbContext
            .Tasks
            .AsNoTracking()
            .Where(t => t.Id == notification.TaskId)
            .SingleOrDefaultAsync(cancellationToken);
        
        if (task is null)
        {
            _logger.LogError("Task {TaskId} not found", notification.TaskId);
            return;
        }

        if (task.Badger is null)
        {
            _logger.LogInformation("Task {TaskId} does not have a badger job", notification.TaskId);
            return;
        }
        
        var scheduleResult = await task.ScheduleBadgerJobAsync(scheduler, cancellationToken);

        if (!scheduleResult.IsSuccess)
        {
            _logger.LogError(
                scheduleResult.Exception, 
                "Failed to schedule badger job for task {TaskId}: {Error}",
                notification.TaskId, 
                scheduleResult.Message);
        }
        
        _logger.LogInformation("Badger job scheduled for task {TaskId}", notification.TaskId);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.Common;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.Jobs;

internal record HydrateJobsFromDatabaseRequest() : IRequest<MoneoResult>;

internal sealed class HydrateJobsFromDatabaseRequestHandler : IRequestHandler<HydrateJobsFromDatabaseRequest, MoneoResult>
{
    private readonly ISchedulerService _schedulerService;
    private readonly MoneoTasksDbContext _dbContext;
    private readonly ILogger<HydrateJobsFromDatabaseRequestHandler> _logger;
    
    public HydrateJobsFromDatabaseRequestHandler(
        ISchedulerService schedulerService, 
        MoneoTasksDbContext dbContext, 
        ILogger<HydrateJobsFromDatabaseRequestHandler> logger)
    {
        _schedulerService = schedulerService;
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<MoneoResult> Handle(HydrateJobsFromDatabaseRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hydrating jobs...");

        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.IsActive == true)
            .ToListAsync(cancellationToken);

        var scheduler = _schedulerService.GetScheduler();
        
        if (scheduler is null)
        {
            return MoneoResult.Failed("Scheduler is not available");
        }

        foreach (var task in tasks)
        {
            var result = await task.ScheduleCheckSendJobAsync(scheduler, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError(
                    result.Exception,
                    "Failed to schedule CHECKSEND for task {TaskId}: {Error}", 
                    task.Id, 
                    result.Message);
            }
            else
            {
                _logger.LogInformation("Scheduled CHECKSEND for task {TaskId}", task.Id);
            }
        }
        
        return MoneoResult.Success();
    }
}

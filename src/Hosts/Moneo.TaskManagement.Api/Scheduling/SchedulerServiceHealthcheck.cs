using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Moneo.TaskManagement.Scheduling;

public class SchedulerServiceHealthcheck : IHealthCheck
{
    private readonly ISchedulerService _schedulerService;
    
    public SchedulerServiceHealthcheck(ISchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        var s = _schedulerService.GetScheduler();
        
        return Task.FromResult(s is { IsStarted: true, InStandbyMode: false }
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy());
    }
}
using MediatR;
using Moneo.TaskManagement.Jobs;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.Scheduling;

namespace Moneo.TaskManagement.Api.Events.ApplicationEvents;

public sealed record ApplicationStartedEvent(DateTime OccurredOn) : INotification;

internal sealed class ApplicationStartedEventHandler : INotificationHandler<ApplicationStartedEvent>
{
    private readonly ILogger<ApplicationStartedEventHandler> _logger;
    private readonly ISender _sender;
    private readonly MoneoTasksDbContext _dbContext;

    public ApplicationStartedEventHandler(
        ILogger<ApplicationStartedEventHandler> logger,
        ISender sender,
        ISchedulerService schedulerService,
        MoneoTasksDbContext dbContext)
    {
        _logger = logger;
        _sender = sender;
        _dbContext = dbContext;
    }

    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application started");
        
        _logger.LogInformation("Ensuring database is created...");
        await _dbContext.Database.EnsureDeletedAsync(cancellationToken);
        var dbExists = await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        
        if (!dbExists)
        {
            _logger.LogInformation("dbExists is false");
        }
        else
        {
            _logger.LogInformation("dbExists is true");
        }

        await _sender.Send(new HydrateJobsFromDatabaseRequest(), cancellationToken);
    }
}

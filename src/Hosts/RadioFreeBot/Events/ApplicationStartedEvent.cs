using MediatR;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Events;

public sealed record ApplicationStartedEvent(DateTime OccurredOn) : INotification;

internal sealed class ApplicationStartedEventHandler : INotificationHandler<ApplicationStartedEvent>
{
    private readonly ILogger<ApplicationStartedEventHandler> _logger;
    private readonly ISender _sender;
    private readonly RadioFreeDbContext _dbContext;

    public ApplicationStartedEventHandler(
        ILogger<ApplicationStartedEventHandler> logger,
        ISender sender,
        RadioFreeDbContext dbContext)
    {
        _logger = logger;
        _sender = sender;
        _dbContext = dbContext;
    }

    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application started");
    
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        if (canConnect)
        {
            _logger.LogInformation("Database exists and is reachable.");
        }
        else
        {
            _logger.LogError("Database does not exist or is not reachable.");
            // exit the application in an error state
            throw new InvalidOperationException("Database connection failed. Please check your configuration.");
        }
    }
}

using MediatR;
using Moneo.TaskManagement.Api.Events.ApplicationEvents;
using Quartz;

namespace Moneo.TaskManagement.Scheduling;

public interface ISchedulerService
{
    IScheduler? GetScheduler();
}

internal class SchedulerService : ISchedulerService, IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ISchedulerFactory _factory;
    private IScheduler? _scheduler;

    public SchedulerService(
        IServiceProvider serviceProvider, 
        TimeProvider timeProvider, 
        ISchedulerFactory factory)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _factory = factory;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _scheduler = await _factory.GetScheduler(cancellationToken);
        await _scheduler.Start(cancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(new ApplicationStartedEvent(_timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _scheduler?.Shutdown(cancellationToken) ?? Task.CompletedTask;
    }

    public IScheduler? GetScheduler() => _scheduler;
}

public class MoneoSchedulerConstants
{
    public const string BadgerGroup = "badger";
    public const string DueGroup = "due";
    public const string ReminderGroup = "reminder";
}

public static class TriggerBuilderExtensions
{
    public static TriggerBuilder WithMoneoIdentity(
        this TriggerBuilder builder, 
        long taskId, 
        CheckSendType sendType,
        int index = 0)
    {
        var typeString = sendType switch
        {
            CheckSendType.Badger => MoneoSchedulerConstants.BadgerGroup,
            CheckSendType.Due => MoneoSchedulerConstants.DueGroup,
            CheckSendType.Reminder => MoneoSchedulerConstants.ReminderGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(sendType), sendType, null)
        };
        
        var triggerName = $"{taskId}-{typeString}-{index}";
        return builder.WithIdentity(triggerName, typeString);
    }
}

public enum CheckSendType
{
    Reminder,
    Due,
    Badger
}

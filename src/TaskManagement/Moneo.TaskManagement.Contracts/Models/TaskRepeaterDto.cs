namespace Moneo.TaskManagement.Contracts.Models;

public record TaskRepeaterDto
{
    public string CronExpression { get; init; }
    public DateTimeOffset? Expiry { get; init; }
    public int EarlyCompletionThresholdHours { get; init; }

    public TaskRepeaterDto(string cronExpression, int earlyCompletionThresholdHours = 3, DateTimeOffset? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(CronExpression));
        }
        
        CronExpression = cronExpression;
        EarlyCompletionThresholdHours = earlyCompletionThresholdHours;
        Expiry = expiry;
    }
}
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

public record TaskRepeater(string CronExpression, int EarlyCompletionThresholdHours = 3, DateTime? Expiry = null)
{
    public static TaskRepeater FromDto(TaskRepeaterDto dto)
    {
        return new TaskRepeater(dto.CronExpression, dto.EarlyCompletionThresholdHours, dto.Expiry?.UtcDateTime);
    }
    
    public TaskRepeaterDto ToDto()
    {
        return new TaskRepeaterDto(CronExpression, EarlyCompletionThresholdHours, Expiry);
    }
}
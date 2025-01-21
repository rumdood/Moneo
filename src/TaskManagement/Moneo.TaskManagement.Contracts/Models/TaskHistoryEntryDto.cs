namespace Moneo.TaskManagement.Contracts.Models;

public class TaskHistoryEntryDto
{
    public long TaskId { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public TaskEventType Type { get; set; }
}
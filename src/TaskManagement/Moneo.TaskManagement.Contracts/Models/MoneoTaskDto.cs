namespace Moneo.TaskManagement.Contracts.Models;

public class MoneoTaskDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DueOn { get; set; }
    public bool CanBeSkipped { get; set; }
    public string Timezone { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<string> CompletedMessages { get; set; } = [];
    public IReadOnlyList<string> SkippedMessages { get; set; } = [];
    public TaskRepeaterDto? Repeater { get; set; }
    public TaskBadgerDto? Badger { get; set; }
}

public class MoneoTaskWithCompletionDataDto : MoneoTaskDto
{
    public DateTimeOffset? LastCompleted { get; set; }
    public DateTimeOffset? LastSkipped { get; set; }
}

public class MoneoTaskWithHistoryDto : MoneoTaskDto
{
    public IReadOnlyList<TaskHistoryEntryDto> History { get; set; } = [];
}

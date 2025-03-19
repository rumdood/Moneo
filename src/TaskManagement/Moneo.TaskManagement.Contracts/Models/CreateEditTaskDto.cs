namespace Moneo.TaskManagement.Contracts.Models;

public record CreateEditTaskDto(
    string Name,
    string Description,
    bool IsActive,
    List<string> CompletedMessages,
    bool CanBeSkipped,
    List<string> SkippedMessages,
    string Timezone,
    DateTimeOffset? DueOn,
    TaskRepeaterDto? Repeater,
    TaskBadgerDto? Badger);

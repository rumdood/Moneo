using Newtonsoft.Json;

namespace Moneo.TaskManagement.Models;

public class TaskReminder
{
    [JsonProperty("reminderDue")]
    public DateTime DueAt { get; set; }

    [JsonProperty("isActive")]
    public bool IsActive { get; set; }

    [JsonProperty("reminderId")]
    public long ReminderId { get => DueAt.Ticks; }
}

using Newtonsoft.Json;

namespace Moneo.Models.TaskManagement;

public class TaskRepeater
{
    [JsonProperty("expiry")]
    public DateTime? Expiry { get; set; }

    [JsonProperty("repeatCron")]
    public string RepeatCron { get; set; } = "";

    [JsonProperty("nextDueDate")]
    public DateTime? NextDueDate { get; set; }

    [JsonProperty("earlyCompletionThreshold")]
    public int? EarlyCompletionThresholdHours { get; set;}
}

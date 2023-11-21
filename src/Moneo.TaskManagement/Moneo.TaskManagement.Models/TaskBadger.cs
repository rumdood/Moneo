using Newtonsoft.Json;

namespace Moneo.TaskManagement.Models;

public class TaskBadger
{
    [JsonProperty("badgerFrequencyMinutes")]
    public int BadgerFrequencyMinutes { get; set; }
    [JsonProperty("badgerMessages")]
    public string[] BadgerMessages { get; set; } = Array.Empty<string>();
}

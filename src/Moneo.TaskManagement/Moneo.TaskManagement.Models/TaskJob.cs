namespace Moneo.TaskManagement.Models;

using Newtonsoft.Json;

public enum JobType
{
    TaskDue,
    Reminder,
    Badger,
}

public class TaskJob
{
    public string Id { get; set; } = "";
    public string TaskId { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime DueDate { get; set; }
    public JobType Type { get; set; }
    public string Message { get; set; } = "";
}
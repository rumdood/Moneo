namespace Moneo.TaskManagement.Models;

using Newtonsoft.Json;

public class TaskJob
{
    public string Id { get; set; } = "";
    public string TaskId { get; set; } = "";
    public bool IsActive { get; set; }
}
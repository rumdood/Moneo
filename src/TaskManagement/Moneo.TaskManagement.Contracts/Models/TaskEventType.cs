namespace Moneo.TaskManagement.Contracts.Models;

public enum TaskEventType
{
    Created = 0,
    Updated = 1,
    Enabled = 2,
    Disabled = 3,
    Completed = 4,
    Skipped = 5,
    Deleted = 6
}
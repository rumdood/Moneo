using Moneo.TaskManagement.Models;

namespace Moneo.Chat.Workflows.CreateTask;

public class MoneoTaskDraft
{
    public bool IsRepeaterEnabled => Task.Repeater is not null;

    public bool IsBadgerEnabled => Task.Badger is not null;

    public MoneoTaskDto Task { get; set; } = new();

    public void EnableRepeater()
    {
        Task.Repeater = new TaskRepeater();
    }
    
    public void DisableRepeater()
    {
        Task.Repeater = null;
    }

    public void EnableBadger()
    {
        Task.Badger = new TaskBadger();
    }

    public void DisableBadger()
    {
        Task.Badger = null;
    }
}
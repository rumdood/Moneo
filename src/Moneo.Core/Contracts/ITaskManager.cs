using System;
using System.Threading.Tasks;
using Moneo.Models;

namespace Moneo.TaskManagement
{
    public interface ITaskManager
    {
        Task MarkCompleted(bool skipped);
        Task Delete();
        Task CheckSendBadger();
        Task CheckTaskCompleted(DateTime dueDate);
        Task SendScheduledReminder(long id);
        Task InitializeTask(MoneoTaskDto task);
        Task UpdateTask(MoneoTaskDto task);
        Task DisableTask();
    }
}

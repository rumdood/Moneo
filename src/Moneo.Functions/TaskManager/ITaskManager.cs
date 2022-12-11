using System;
using System.Threading.Tasks;
using Moneo.Models;

namespace Moneo.Functions
{
    public interface ITaskManager
    {
        Task MarkCompleted(bool skipped);
        Task Delete();
        Task CheckSendBadger();
        Task CheckTaskCompleted(DateTime dueDate);
        Task SendScheduledReminder(long id);
        Task InitializeTask(MoneoTaskCreateModel task);
        Task UpdateTask(MoneoTaskDto task);
        Task DisableTask();
    }
}

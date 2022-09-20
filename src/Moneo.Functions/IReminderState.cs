using System;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    public interface ITaskManager
    {
        Task MarkCompleted(bool skipped);
        Task<MoneoTask> GetTaskDetail();
        Task Delete();
        Task CheckSendReminder();
        Task InitializeTask(MoneoTask task);
        Task UpdateTask(MoneoTask task);
        Task DisableTask();
    }
}

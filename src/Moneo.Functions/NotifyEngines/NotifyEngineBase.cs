using System;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    public abstract class NotifyEngineBase
    {
        public virtual Task SendDefuseMessage()
        {
            var defuseMesage = Environment.GetEnvironmentVariable("defusedMessage", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("Defuse Message Not Found");

            return SendNotification(defuseMesage);
        }

        public abstract Task SendNotification(string message);

        public virtual Task SendReminder()
        {
            var reminderMessage = Environment.GetEnvironmentVariable("reminderMessage", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("Reminder Message Not Found");

            return SendNotification(reminderMessage);
        }
    }
}

using System;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    public abstract class NotifyEngineBase
    {
        public virtual Task SendCompletedMessage()
        {
            var defuseMesage = Environment.GetEnvironmentVariable("completedMessage", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("Completed Message Setting Not Found");

            return SendNotification(defuseMesage);
        }

        public virtual Task SendSkippedMessage()
        {
            var defuseMesage = Environment.GetEnvironmentVariable("skippedMessage", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("Skipped Message Setting Not Found");

            return SendNotification(defuseMesage);
        }

        public abstract Task SendNotification(string message);

        public virtual Task SendReminder()
        {
            var reminderMessage = Environment.GetEnvironmentVariable("reminderMessage", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("Reminder Message Setting Not Found");

            return SendNotification(reminderMessage);
        }
    }
}

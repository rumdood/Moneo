using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moneo.Models;
using Moneo.TaskManagement;
using Moneo.Notify;
using Moneo.Core;
using System.Linq;

namespace Moneo.Functions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TaskManager: ITaskManager
    {
        private readonly INotifyEngine _notifier;
        private readonly IMoneoTaskFactory _taskFactory;
        private readonly IScheduleManager _scheduleManager;
        private readonly Random _random = new();

        [JsonProperty("task")]
        public MoneoTaskWithReminders TaskState { get; set;}
        public HashSet<DateTime> ScheduledDueDates { get; set; } = new HashSet<DateTime>();

        private async Task CheckSend(string message, bool badgerFlag = false)
        {
            var threshold = MoneoConfiguration.DefuseThresholdHours;

            if (TaskState is null || !TaskState.IsActive)
            {
                // skip the check and wait for cleanup
                return;
            }

            if ((TaskState.CompletedOn.HasValue && DateTime.UtcNow.Subtract(TaskState.CompletedOn.Value).TotalHours < threshold) ||
                (TaskState.SkippedOn.HasValue && DateTime.UtcNow.Subtract(TaskState.SkippedOn.Value).TotalHours < threshold))
            {
                if (TaskState.IsActive && TaskState.Repeater is not null && TaskState.Repeater.Expiry.HasValue && TaskState.Repeater.Expiry.Value < DateTime.UtcNow)
                {
                    // disable the expired task if we wouldn't remind them of it now
                    TaskState.IsActive = false;
                }
                return;
            }

            await _notifier.SendNotification(message);

            // if there's a repeater, schedule the next go-round
            if (TaskState.Repeater is not null)
            {
                /// TODO: Criteria for removing old/expired duedates, for now, anything older than X days
                ScheduledDueDates.RemoveWhere(d => DateTime.Now.Subtract(d).TotalDays > MoneoConfiguration.OldDueDatesMaxDays);
                TaskState.DueDates = _scheduleManager.GetDueDates(TaskState).ToHashSet();
                UpdateSchedule();
            }

            if (TaskState.Badger is null || !badgerFlag)
            {
                return;
            }

            // schedule a follow-up
            Entity.Current.SignalEntity<ITaskManager>(Entity.Current.EntityId,
                DateTime.UtcNow.AddMinutes(TaskState.Badger.BadgerFrequencyMinutes),
                e => e.CheckSendBadger());
        }

        private void UpdateSchedule()
        {
            _ = ScheduledDueDates.RemoveWhere(item => !TaskState.DueDates.Contains(item));

            foreach (var dueDate in TaskState.DueDates)
            {
                if (ScheduledDueDates.Contains(dueDate))
                {
                    continue;
                }

                ScheduledDueDates.Add(dueDate);

                Entity.Current.SignalEntity<ITaskManager>(
                    Entity.Current.EntityId,
                    dueDate,
                    e => e.CheckTaskCompleted(dueDate));
            }
        }

        private static string GetReminderMessage(IMoneoTask task)
        {
            return MoneoConfiguration.DefaultReminderMessage
                .Replace("[TaskName]", task.Name)
                .Replace("[TaskDue]", task.DueDates.First().ToString("f"));
        }

        private static string GetTaskDueMessage(IMoneoTask task)
        {
            return MoneoConfiguration.DefaultTaskDueMessage.Replace("[TaskName]", task.Name);
        }

        public TaskManager(
            INotifyEngine notifier, 
            IMoneoTaskFactory moneoTaskFactory, 
            IScheduleManager scheduleManager)
        {
            _notifier = notifier;
            _taskFactory = moneoTaskFactory;
            _scheduleManager = scheduleManager;
        }

        public Task InitializeTask(MoneoTaskDto task)
        {
            if (TaskState is { IsActive: true })
            {
                throw new InvalidOperationException("Active task already exists");  
            }

            return UpdateTask(task);
        }

        public Task UpdateTask(MoneoTaskDto task)
        {
            if (task.DueDates.Count == 0)
            {
                throw new InvalidOperationException("Tasks must have at least one due date");
            }

            var existingReminders = TaskState?.Reminders;

            TaskState = _taskFactory.CreateTaskWithReminders(task);

            UpdateSchedule();

            foreach (var reminder in task.Reminders.EmptyIfNull())
            {
                if (existingReminders is not null && existingReminders.ContainsKey(reminder.UtcTicks) || reminder.UtcDateTime <= DateTime.UtcNow)
                {
                    continue;
                }

                Entity.Current.SignalEntity<ITaskManager>(
                    Entity.Current.EntityId, 
                    reminder.UtcDateTime, 
                    e => e.SendScheduledReminder(reminder.UtcTicks));
            }

            return Task.CompletedTask;
        }

        public async Task MarkCompleted(bool skipped = false)
        {
            if (!TaskState.IsActive)
            {
                throw new InvalidOperationException("Task is not active");
            }

            if (skipped)
            {
                TaskState.SkippedOn = DateTime.UtcNow;
                await _notifier.SendNotification(TaskState.SkippedMessage ?? 
                    MoneoConfiguration.DefaultSkippedMessage.Replace("[TaskName]", TaskState.Name));
                return;
            }

            TaskState.CompletedOn = DateTime.UtcNow;
            await _notifier.SendNotification(TaskState.CompletedMessage ?? 
                MoneoConfiguration.DefaultCompletedMessage.Replace("[TaskName]", TaskState.Name));
        }

        public Task DisableTask()
        {
            if (TaskState is {  IsActive: true })
            {
                TaskState.IsActive = false;
            }

            return Task.CompletedTask;
        }

        public Task Delete()
        {
            DisableTask();
            Entity.Current.DeleteState();
            return Task.CompletedTask;
        }

        public Task CheckSendBadger() => CheckSend(TaskState.Badger.BadgerMessages[_random.Next(0, TaskState.Badger.BadgerMessages.Length)], true);

        public Task CheckTaskCompleted(DateTime dueDate)
        {
            if (!ScheduledDueDates.Contains(dueDate))
            {
                return Task.CompletedTask;
            }

            return CheckSend(GetTaskDueMessage(TaskState), true);
        }

        public Task SendScheduledReminder(long id)
        {
            // skip any reminders that have been removed
            if (!TaskState.Reminders.ContainsKey(id))
            {
                return Task.CompletedTask;
            }

            return CheckSend(GetReminderMessage(TaskState), false);
        }

        [FunctionName(nameof(TaskManager))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) =>
            context.DispatchAsync<TaskManager>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Moneo.Models;
using Moneo.Notify;
using Moneo.TaskManagement;
using Newtonsoft.Json;

namespace Moneo.Functions;

[JsonObject(MemberSerialization.OptIn)]
public class TaskManager : ITaskManager
{
    private readonly INotifyEngine _notifier;
    private readonly IMoneoTaskFactory _taskFactory;
    private readonly IScheduleManager _scheduleManager;
    private readonly Random _random = new();
    private readonly ILogger<TaskManager> _logger;

    [JsonProperty("task")] 
    public MoneoTaskState TaskState { get; set; }
    [JsonProperty("scheduledChecks")] 
    public HashSet<DateTime> ScheduledDueDates { get; set; } = new();

    private static bool IsQuietHours()
    {
        var timezoneString = MoneoConfiguration.QuietHours.TimeZone;
        var now = DateTime.Now.GetTimeZoneAdjustedDateTime(timezoneString);

        return now > MoneoConfiguration.QuietHours.Start.GetTimeZoneAdjustedDateTime(timezoneString)
            && now < MoneoConfiguration.QuietHours.End.GetTimeZoneAdjustedDateTime(timezoneString);
    }

    private async Task CheckSend(string message, bool badgerFlag = false)
    {
        _logger.LogDebug(
            "Performing CheckSend for [{0}] {1}", 
            this.TaskState.Name, 
            badgerFlag ? "(badgering)" : "");

        if (TaskState is null || !TaskState.IsActive)
        { 
            // skip the check and wait for cleanup
            return;
        }

        var (_, _, _, repeater, badger) = TaskState;
        var completedOrSkipped = TaskState.GetLastCompletedOrSkippedDate();

        if (completedOrSkipped is not null)
        {
            _logger.LogDebug("    Last Completed/Skipped on {0}", completedOrSkipped.Value);

            switch (repeater)
            {
                case null:
                    await DisableTask();
                    return;
                case {EarlyCompletionThresholdHours: var taskThreshold, Expiry: var expiry} when !taskThreshold.HasValue
                    || completedOrSkipped?.HoursSince(DateTime.UtcNow) < taskThreshold.Value:
                {
                    if (expiry.HasValue && expiry.Value < DateTime.UtcNow)
                    {
                        _logger.LogDebug("    Diabling Expired Task");
                        await DisableTask();
                    }
                    return;
                }
            }
        }

        if (!IsQuietHours())
        { 
            _logger.LogDebug("    Sending Notification");
            await _notifier.SendNotification(message);
        }

        // if there's a repeater, schedule the next go-round
        if (repeater is not null)
        {
            _logger.LogDebug("    Schedule Repeater");
            // TODO: Criteria for removing old/expired duedates, for now, anything older than X days
            ScheduledDueDates.RemoveWhere(d =>
                DateTime.Now.Subtract(d).TotalDays > MoneoConfiguration.OldDueDatesMaxDays);
            TaskState.DueDates = _scheduleManager.GetDueDates(TaskState).ToHashSet();
            UpdateSchedule();
        }

        if (badger is null || !badgerFlag) return;

        _logger.LogDebug("    Schedule badger");
        // schedule a follow-up
        Entity.Current.SignalEntity<ITaskManager>(Entity.Current.EntityId,
            DateTime.UtcNow.AddMinutes(TaskState.Badger!.BadgerFrequencyMinutes),
            e => e.CheckSendBadger());
    }

    private void UpdateSchedule()
    {
        _logger.LogDebug("Updating schedule for [{0}]", this.TaskState.Name);

        var keepers = _scheduleManager.GetKeepers(TaskState, ScheduledDueDates);
        ScheduledDueDates.RemoveWhere(dueDate => !keepers.Contains(dueDate));

        foreach (var dueDate in TaskState.DueDates.Where(dueDate => !ScheduledDueDates.Contains(dueDate)))
        {
            _logger.LogDebug("    Scheduling CheckSend for DueDate [{0}]", dueDate);

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
        IScheduleManager scheduleManager,
        ILogger<TaskManager> logger)
    {
        _notifier = notifier;
        _taskFactory = moneoTaskFactory;
        _scheduleManager = scheduleManager;
        _logger = logger;
    }

    public Task InitializeTask(MoneoTaskDto task)
    {
        _logger.LogDebug("Initializing new task [{0}]", task.Name);

        if (TaskState is {IsActive: true})
        {
            throw new InvalidOperationException("Active task already exists");
        }

        return UpdateTask(task);
    }

    public Task UpdateTask(MoneoTaskDto task)
    {
        _logger.LogDebug("Updating Task [{0}]", task.Name);

        var existingReminders = TaskState?.Reminders;

        TaskState = _taskFactory.CreateTaskWithReminders(task, TaskState);

        UpdateSchedule();

        foreach (var reminder in task.Reminders.EmptyIfNull())
        {
            if ((existingReminders is not null && existingReminders.ContainsKey(reminder.UtcTicks)) ||
                reminder.UtcDateTime <= DateTime.UtcNow) continue;

            _logger.LogDebug("    Scheduling Reminder for {0}", reminder.UtcDateTime);

            Entity.Current.SignalEntity<ITaskManager>(
                Entity.Current.EntityId,
                reminder.UtcDateTime,
                e => e.SendScheduledReminder(reminder.UtcTicks));
        }

        return Task.CompletedTask;
    }

    public async Task MarkCompleted(bool skipped = false)
    {
        _logger.LogDebug("{0} Task [{1}]", skipped ? "Skipping" : "Completing", this.TaskState.Name);

        if (!TaskState.IsActive)
        {
            throw new InvalidOperationException("Task is not active");
        }

        if (TaskState.Repeater is null)
        {
            await DisableTask();
        }

        if (skipped)
        {
            TaskState.SkippedHistory.Add(DateTime.UtcNow);
            await _notifier.SendNotification(TaskState.SkippedMessage ??
                                             MoneoConfiguration.DefaultSkippedMessage.Replace("[TaskName]",
                                                 TaskState.Name));
            return;
        }

        TaskState.CompletedHistory.Add(DateTime.UtcNow);
        await _notifier.SendNotification(TaskState.CompletedMessage ??
                                         MoneoConfiguration.DefaultCompletedMessage.Replace("[TaskName]",
                                             TaskState.Name));
    }

    public Task DisableTask()
    {
        if (TaskState is {IsActive: true})
        {
            TaskState.IsActive = false;
        }

        return Task.CompletedTask;
    }

    public Task Delete()
    {
        Entity.Current.DeleteState();
        return Task.CompletedTask;
    }

    public Task CheckSendBadger()
    {
        _logger.LogDebug("Sending Badger for [{0}]", this.TaskState.Name);

        if (TaskState?.Badger is null)
        {
            throw new InvalidOperationException("Cannot use null Badger reference to send a badger message");
        }
        
        return CheckSend(
            TaskState.Badger.BadgerMessages[_random.Next(0, TaskState.Badger.BadgerMessages.Length)],
            true);
    }

    public Task CheckTaskCompleted(DateTime dueDate)
    {
        return !ScheduledDueDates.Contains(dueDate)
            ? Task.CompletedTask
            : CheckSend(GetTaskDueMessage(TaskState), true);
    }

    public Task SendScheduledReminder(long id)
    {
        // skip any reminders that have been removed
        return !TaskState.Reminders.ContainsKey(id) 
            ? Task.CompletedTask 
            : CheckSend(GetReminderMessage(TaskState));
    }

    [FunctionName(nameof(TaskManager))]
    public static Task Run([EntityTrigger] IDurableEntityContext context)
    {
        return context.DispatchAsync<TaskManager>();
    }
}
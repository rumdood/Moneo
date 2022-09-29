using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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

    [JsonProperty("task")] public MoneoTaskState TaskState { get; set; }
    [JsonProperty("scheduledChecks")] public HashSet<DateTime> ScheduledDueDates { get; set; } = new();

    private async Task CheckSend(string message, bool badgerFlag = false)
    {
        if (TaskState is null || !TaskState.IsActive)
            // skip the check and wait for cleanup
            return;

        var (_, _, _, repeater, badger) = TaskState;
        var completedOrSkipped = TaskState.GetLastCompletedOrSkippedDate();

        if (completedOrSkipped != default)
        {
            switch (repeater)
            {
                case null:
                    await DisableTask();
                    return;
                case {EarlyCompletionThresholdHours: var taskThreshold, Expiry: var expiry} when !taskThreshold.HasValue
                    || completedOrSkipped.HoursSince(DateTime.UtcNow) < taskThreshold.Value:
                {
                    if (expiry.HasValue && expiry.Value < DateTime.UtcNow) await DisableTask();
                    return;
                }
            }
        }

        await _notifier.SendNotification(message);

        // if there's a repeater, schedule the next go-round
        if (repeater is not null)
        {
            // TODO: Criteria for removing old/expired duedates, for now, anything older than X days
            ScheduledDueDates.RemoveWhere(d =>
                DateTime.Now.Subtract(d).TotalDays > MoneoConfiguration.OldDueDatesMaxDays);
            TaskState.DueDates = _scheduleManager.GetDueDates(TaskState).ToHashSet();
            UpdateSchedule();
        }

        if (badger is null || !badgerFlag) return;

        // schedule a follow-up
        Entity.Current.SignalEntity<ITaskManager>(Entity.Current.EntityId,
            DateTime.UtcNow.AddMinutes(TaskState.Badger!.BadgerFrequencyMinutes),
            e => e.CheckSendBadger());
    }

    private void UpdateSchedule()
    {
        ScheduledDueDates = _scheduleManager.MergeDueDates(TaskState, ScheduledDueDates).ToHashSet();

        foreach (var dueDate in TaskState.DueDates.Where(dueDate => !ScheduledDueDates.Contains(dueDate)))
        {
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
        if (TaskState is {IsActive: true})
        {
            throw new InvalidOperationException("Active task already exists");
        }

        return UpdateTask(task);
    }

    public Task UpdateTask(MoneoTaskDto task)
    {
        var existingReminders = TaskState?.Reminders;

        TaskState = _taskFactory.CreateTaskWithReminders(task, TaskState);

        UpdateSchedule();

        foreach (var reminder in task.Reminders.EmptyIfNull())
        {
            if ((existingReminders is not null && existingReminders.ContainsKey(reminder.UtcTicks)) ||
                reminder.UtcDateTime <= DateTime.UtcNow) continue;

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

        if (TaskState.Repeater is null)
        {
            await DisableTask();
        }

        if (skipped)
        {
            TaskState.LastSkippedOn = DateTime.UtcNow;
            await _notifier.SendNotification(TaskState.SkippedMessage ??
                                             MoneoConfiguration.DefaultSkippedMessage.Replace("[TaskName]",
                                                 TaskState.Name));
            return;
        }

        TaskState.LastCompletedOn = DateTime.UtcNow;
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
        return !TaskState.Reminders.ContainsKey(id) ? Task.CompletedTask : CheckSend(GetReminderMessage(TaskState));
    }

    [FunctionName(nameof(TaskManager))]
    public static Task Run([EntityTrigger] IDurableEntityContext context)
    {
        return context.DispatchAsync<TaskManager>();
    }
}
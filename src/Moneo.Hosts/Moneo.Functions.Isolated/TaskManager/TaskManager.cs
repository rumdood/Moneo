using Moneo.TaskManagement.Models;
using Microsoft.Azure.Functions.Worker;
using Moneo.TaskManagement;
using Microsoft.Extensions.Logging;
using Moneo.Core;
using Microsoft.DurableTask.Entities;
using Microsoft.DurableTask;


namespace Moneo.Functions.Isolated.TaskManager
{
    internal class TaskManager : TaskEntity<MoneoTaskState>, ITaskManager
    {
        private readonly INotifyEngine _notifier;
        private readonly IMoneoTaskFactory _taskFactory;
        private readonly IScheduleManager _scheduleManager;
        private readonly Random _random = new();
        private readonly ILogger<TaskManager> _logger;

        protected override bool AllowStateDispatch => base.AllowStateDispatch;

        private static bool IsQuietHours()
        {
            var timezoneString = MoneoConfiguration.QuietHours.TimeZone;
            var now = DateTime.UtcNow;

            return now > MoneoConfiguration.QuietHours.Start.ToUniversalTime(timezoneString)
                && now < MoneoConfiguration.QuietHours.End.ToUniversalTime(timezoneString);
        }

        private async Task CheckSend(string message, bool badgerFlag = false)
        {
            _logger.LogTrace(
                "[{Method}] Performing CheckSend for [{Name}] {BadgerFlag}",
                nameof(CheckSend),
                State.Name,
                badgerFlag ? "(badgering)" : "");

            if (State is null || !State.IsActive)
            {
                // skip the check and wait for cleanup
                return;
            }

            var (repeater, badger) = (State.Repeater, State.Badger);
            var completedOrSkipped = State.GetLastCompletedOrSkippedDate();

            if (repeater is { EarlyCompletionThresholdHours: var threshold, Expiry: var expiry })
            {
                if (expiry.HasValue && expiry.Value < DateTime.UtcNow && completedOrSkipped is not null)
                {
                    await DisableTask();
                    return; // task is completed and expired, disable and close
                }
                else
                {
                    _logger.LogTrace("[{Method}] Schedule Repeater", nameof(CheckSend));
                    // TODO: Criteria for removing old/expired due-dates, for now, anything older than X days
                    State.ScheduledChecks.RemoveWhere(d =>
                        DateTime.Now.Subtract(d).TotalDays > MoneoConfiguration.OldDueDatesMaxDays);
                    State.DueDates = _scheduleManager.GetDueDates(State).ToHashSet();
                    UpdateSchedule();

                    if (completedOrSkipped is not null
                        && !threshold.HasValue || (threshold.HasValue &&
                                                   completedOrSkipped?.HoursSince(DateTime.UtcNow) < threshold.Value))
                    {
                        return; // don't continue to the badger
                    }
                }
            }
            else
            {
                // if there is no repeater and the task has been completed, disable the task and return
                if (completedOrSkipped is not null)
                {
                    await DisableTask();
                    return;
                }
            }

            if (!IsQuietHours())
            {
                _logger.LogTrace("[{Method}] Sending Notification", nameof(CheckSend));
                await _notifier.SendNotification(State.ConversationId, message);
            }

            if (badger is null || !badgerFlag) return;

            _logger.LogTrace("[{Method}] Schedule badger", nameof(CheckSend));

            // schedule a follow-up
            Context.SignalEntity(
                id: Context.Id,
                operationName: nameof(CheckSendBadger),
                options: new SignalEntityOptions
                {
                    SignalTime = DateTime.UtcNow.AddMinutes(State.Badger!.BadgerFrequencyMinutes)
                });

            /*
            Context.ScheduleNewOrchestration(
                nameof(CheckSendBadger),
                new StartOrchestrationOptions
                {
                    StartAt = DateTime.UtcNow.AddMinutes(State.Badger!.BadgerFrequencyMinutes)
                });
            */
        }

        private void UpdateSchedule()
        {
            _logger.LogTrace("Updating schedule for [{@Name}]", State.Name);

            if (State.Repeater is not null)
            {
                var datesToRemove = State.ScheduledChecks.Where(dueDate => !State.DueDates.Contains(dueDate));
                _logger.LogTrace("The following DueDates will be removed: {D}", datesToRemove);

                // remove any scheduled items that haven't been carried over
                State.ScheduledChecks.RemoveWhere(dueDate => !State.DueDates.Contains(dueDate));
            }

            foreach (var dueDate in State.DueDates.Where(dueDate => !State.ScheduledChecks.Contains(dueDate)))
            {
                // schedule and add new DueDates that aren't already scheduled
                _logger.LogTrace("Scheduling CheckSend for DueDate [{@DueDate}]", dueDate);

                State.ScheduledChecks.Add(dueDate);

                /* why doesn't this work? why am i doing this orc thing?
                Context.SignalEntity(
                    id: Context.Id,
                    operationName: nameof(CheckTaskCompleted),
                    input: dueDate,
                    options: new SignalEntityOptions
                    {
                        SignalTime = dueDate,
                    }
                 );
                */

                Context.ScheduleNewOrchestration(
                    nameof(CheckTaskCompletedOrc),
                    new CheckCompletedRequest(Context.Id, dueDate)
                );
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

        protected override MoneoTaskState InitializeState(TaskEntityOperation entityOperation)
        {
            return new();
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
            _logger.LogTrace("Initializing new task [{@Name}]", task.Name);

            if (State is { IsActive: true })
            {
                throw new InvalidOperationException("Active task already exists");
            }

            var fullId = TaskFullId.CreateFromFullId(Context.Id.Key);

            if (task.ConversationId == default)
            {
                throw new InvalidOperationException("Conversation Id is required");
            }

            if (string.IsNullOrEmpty(task.Id))
            {
                task.Id = fullId.TaskId;
            }

            return UpdateTask(task);
        }

        public Task UpdateTask(MoneoTaskDto task)
        {
            _logger.LogTrace("Updating Task [{@Name}]", task.Name);

            var existingReminders = State?.Reminders;

            State = _taskFactory.CreateTaskWithReminders(task, State, MoneoConfiguration.MaxCompletionHistoryEventCount);

            UpdateSchedule();

            foreach (var reminder in task.Reminders.EmptyIfNull())
            {
                if (existingReminders is not null && existingReminders.ContainsKey(reminder.UtcTicks) ||
                    reminder.UtcDateTime <= DateTime.UtcNow) continue;

                _logger.LogTrace("    Scheduling Reminder for {@Reminder}", reminder.UtcDateTime);

                Context.ScheduleNewOrchestration(
                    nameof(SendScheduledReminder),
                    reminder.UtcDateTime.Ticks,
                    new Microsoft.DurableTask.StartOrchestrationOptions
                    {
                        StartAt = reminder.UtcDateTime
                    });
            }

            return Task.CompletedTask;
        }

        public async Task MarkCompleted(bool skipped = false)
        {
            _logger.LogTrace("{@Action} Task [{@Name}]", skipped ? "Skipping" : "Completing", State.Name);

            if (!State.IsActive)
            {
                throw new InvalidOperationException("Task is not active");
            }

            if (State.Repeater is null)
            {
                await DisableTask();
            }

            if (skipped)
            {
                State.SkippedHistory.Add(DateTime.UtcNow);
                await _notifier.SendNotification(
                    State.ConversationId,
                    State.SkippedMessage ?? MoneoConfiguration.DefaultSkippedMessage.Replace("[TaskName]",
                    State.Name));
                return;
            }

            State.CompletedHistory.Add(DateTime.UtcNow);
            await _notifier.SendNotification(
                State.ConversationId,
                State.CompletedMessage ?? MoneoConfiguration.DefaultCompletedMessage.Replace("[TaskName]",
                State.Name));
        }

        public Task DisableTask()
        {
            State.IsActive = false;
            return Task.CompletedTask;
        }

        public Task Delete()
        {
            Context.SignalEntity(Context.Id, nameof(DisableTask));
            return Task.CompletedTask;
        }

        [Function(nameof(CheckSendBadgerOrc))]
        public async Task CheckSendBadgerOrc([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var id = context.GetInput<string>();

            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("Id is null or empty");
            }

            var entityId = new EntityInstanceId(nameof(MoneoTaskState), id);
            await context.Entities.SignalEntityAsync(entityId, nameof(CheckSendBadger));
        }

        public Task CheckSendBadger()
        {
            _logger.LogTrace("Sending Badger for [{@Name}]", State.Name);

            if (State?.Badger is null)
            {
                throw new InvalidOperationException("Cannot use null Badger reference to send a badger message");
            }

            return CheckSend(
                State.Badger.BadgerMessages[_random.Next(0, State.Badger.BadgerMessages.Length)],
                true);
        }

        [Function(nameof(CheckTaskCompletedOrc))]
        public async static Task CheckTaskCompletedOrc([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var request = context.GetInput<CheckCompletedRequest>();

            if (request is null)
            {
                throw new InvalidOperationException("Request is null");
            }

            var (entityId, dueDate) = request;
            await context.Entities.SignalEntityAsync(entityId, nameof(CheckTaskCompleted), dueDate);
        }

        public Task CheckTaskCompleted(DateTime dueDate)
        {
            if (!State.ScheduledChecks.Contains(dueDate))
            {
                _logger.LogTrace("Checked Due Date not in schedule, CheckSend will not be called: {D}",
                    dueDate.ToLongDateString());
            }

            return !State.ScheduledChecks.Contains(dueDate)
                ? Task.CompletedTask
                : CheckSend(GetTaskDueMessage(State), true);
        }

        public Task SendScheduledReminder(long id)
        {
            // skip any reminders that have been removed
            return !State.Reminders.ContainsKey(id)
                ? Task.CompletedTask
                : CheckSend(GetReminderMessage(State));
        }

        [Function(nameof(MoneoTaskState))]
        public Task DispatchAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
        {
            return dispatcher.DispatchAsync(this);
        }

        public Task PerformMigrationAction()
        {
            var fullId = TaskFullId.CreateFromFullId(Context.Id.Key);
            State.Id = fullId.TaskId;
            State.ConversationId = long.Parse(fullId.ChatId);

            _logger.LogInformation("Setting Task State Id: {@TaskId}", fullId.TaskId);

            return Task.CompletedTask;
        }
    }
}

internal static class TaskEntityContextExtensions
{
    internal static long GetChatIdFromEntityId(this TaskEntityContext context)
    {
        if (!long.TryParse(context.Id.Key.Split('_').First(), out var chatId))
        {
            throw new InvalidOperationException($"Cannot retrieve Chat Id from entity key [{context.Id.Key}]");
        }

        return chatId;
    }
}

internal record CheckCompletedRequest(EntityInstanceId EntityInstanceId, DateTime DueDate);

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TaskManager: ITaskManager
    {
        private readonly INotifyEngine _notifier;
        private MoneoTask _state;

        private readonly HashSet<TimeOnly> _scheduleItems = new();

        public DateTime? LastDefused
        {
            get => _state.LastCompleted;
        }

        public bool IsActive
        {
            get => _state.IsActive;
        }

        public TaskManager(INotifyEngine notifier)
        {
            _notifier = notifier;
        }

        public async Task InitializeTask(MoneoTask task)
        {
            if (_state.IsActive)
            {
                throw new InvalidOperationException("Active task already exists");
            }

            await UpdateTask(task);
        }

        public Task UpdateTask(MoneoTask task)
        {
            _state = task;

            if (_scheduleItems.Count > 0)
            {
                var removable = _scheduleItems.Except(_state.ScheduledChecks);

                foreach (var remove in removable)
                {
                    _scheduleItems.Remove(remove);
                }
                
                var missing = _state.ScheduledChecks.Where(item => !_scheduleItems.Contains(item));
                _scheduleItems.UnionWith(missing);
            }

            return Task.CompletedTask;
        }

        public async Task MarkCompleted(bool skipped = false)
        {
            if (!_state.IsActive)
            {
                throw new InvalidOperationException("Task is not active");
            }

            if (skipped)
            {
                _state.LastSkipped = DateTime.UtcNow;
                await _notifier.SendNotification(_state.SkippedMessage ?? 
                    Environment.GetEnvironmentVariable("defaulteSkippedMessage").Replace("[TaskName]", _state.Name));
                return;
            }

            _state.LastCompleted = DateTime.UtcNow;
            await _notifier.SendNotification(_state.CompletedMessage ?? 
                Environment.GetEnvironmentVariable("defaultCompletedMessage").Replace("[TaskName]", _state.Name));
        }

        public Task DisableTask()
        {
            _state.IsActive = false;
            return Task.CompletedTask;
        }

        [FunctionName(nameof(TaskManager))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) => 
            context.DispatchAsync<TaskManager>();

        public Task<MoneoTask> GetTaskDetail()
        {
            return Task.FromResult(_state);
        }

        public async Task CheckSendReminder()
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable("defuseThresholdHours"), out var threshold))
            {
                threshold = 4;
            }

            if (!_state.IsActive)
            {
                // skip the check and wait for cleanup
                return;
            }

            if ((_state.LastCompleted.HasValue && DateTime.UtcNow.Subtract(_state.LastCompleted.Value).TotalHours < threshold) ||
                (_state.LastSkipped.HasValue && DateTime.UtcNow.Subtract(_state.LastSkipped.Value).TotalHours < threshold))
            {
                if (_state.IsActive && _state.Expiry.HasValue && _state.Expiry.Value < DateTime.UtcNow)
                {
                    // disable the expired task if we wouldn't remind them of it now
                    _state.IsActive = false;
                }
                return;
            }

            await _notifier.SendNotification(_state.ReminderMessage ?? 
                Environment.GetEnvironmentVariable("defaultReminderMessage").Replace("[TaskName]", _state.Name));

            // schedule a follow-up
            Entity.Current.SignalEntity<ITaskManager>(Entity.Current.EntityId, 
                DateTime.UtcNow.AddMinutes(_state.NagFrequencyMinutes), 
                e => e.CheckSendReminder());
        }

        public Task Delete()
        {
            Entity.Current.DeleteState();
            return Task.CompletedTask;
        }
    }
}

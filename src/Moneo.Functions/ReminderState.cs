using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ReminderState: IReminderState
    {
        private readonly INotifyEngine _notifier;

        public ReminderState(INotifyEngine notifier)
        {
            _notifier = notifier;
        }

        [JsonProperty("lastDefused")]
        public DateTime LastDefused { get; private set; }

        public async Task Defuse()
        {
            LastDefused = DateTime.UtcNow;
            await _notifier.SendNotification(Environment.GetEnvironmentVariable("defusedMessage"));
        }

        [FunctionName(nameof(ReminderState))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) => 
            context.DispatchAsync<ReminderState>();

        public Task<DateTime> GetLastDefusedTimestamp()
        {
            return Task.FromResult(LastDefused);
        }

        public async Task CheckSendReminder()
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable("defuseThresholdHours"), out var threshold))
            {
                threshold = 1;
            }

            if (!int.TryParse(Environment.GetEnvironmentVariable("followUpTimerMinutes"), out var followUpMinutes))
            {
                followUpMinutes = 60;
            }

            if (DateTime.UtcNow.Subtract(LastDefused).TotalHours <= threshold)
            {
                return;
            }

            await _notifier.SendNotification(Environment.GetEnvironmentVariable("reminderMessage"));

            // schedule a follow-up
            Entity.Current.SignalEntity<IReminderState>(Entity.Current.EntityId, 
                DateTime.UtcNow.AddMinutes(followUpMinutes), 
                e => e.CheckSendReminder());
        }

        public Task Delete()
        {
            Entity.Current.DeleteState();
            return Task.CompletedTask;
        }
    }
}

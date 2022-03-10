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
        [JsonProperty("lastTaken")]
        public DateTime LastDefused { get; private set; }

        public void Defuse()
        {
            LastDefused = DateTime.UtcNow;
        }

        [FunctionName(nameof(ReminderState))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) => 
            context.DispatchAsync<ReminderState>();

        public Task<DateTime> GetLastDefusedTimestamp()
        {
            return Task.FromResult(LastDefused);
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
        }
    }
}

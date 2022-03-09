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
        public DateTime LastTaken { get; private set; }

        public void Defuse()
        {
            LastTaken = DateTime.UtcNow;
        }

        [FunctionName(nameof(ReminderState))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) => 
            context.DispatchAsync<ReminderState>();
    }
}

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moneo.Functions.Bot;
using Moneo.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moneo.Functions;

public interface IConversation
{
    Task<States> GetState();
    void SetState(States state);
}

[JsonObject(MemberSerialization.OptIn)]
public class Conversation : IConversation
{
    [JsonProperty("State")]
    public States State { get; set; }

    [JsonProperty("ConversationId")]
    public long ConversationId { get; set;}

    [JsonProperty("CurrentTask")]
    public MoneoTaskDto CurrentTask { get; set;}

    public Task<States> GetState() => Task.FromResult(State);
    public void SetState(States state) => State = state;

    public BotResponse CreateTask()
    {
        if (this.State != States.Main)
        {
            return new BotResponse("You're already creating a task, finish that one first", this.ConversationId);
        }

        Entity.Current.StartNewOrchestration(nameof(OrchestrationFunctions.RunOrchestrator), Entity.Current.EntityId);
        return default;
    }

    [FunctionName(nameof(Conversation))]
    public static Task Run([EntityTrigger] IDurableEntityContext context)
    {
        if (!context.HasState)
        {
            context.SetState(new Conversation { State = States.Main });
        }

        return context.DispatchAsync<Conversation>();
    }
}

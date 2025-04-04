using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.DomainEvents;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

[Table("tasks")]
[Index(nameof(Name), nameof(ConversationId), IsUnique = true)]
public class MoneoTask : AuditableEntity, IHasDomainEvents
{
    [NotMapped] private List<string>? _completedMessages;
    [NotMapped] private List<string>? _skippedMessages;
    [NotMapped] private TaskBadger? _badger;
    [NotMapped] private TaskRepeater? _repeater;
    
    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Required]
    [Column("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [Column("canBeSkipped")] 
    public bool CanBeSkipped { get; set; } = true;

    [Column("timezone")]
    [Required]
    [StringLength(100)]
    public string Timezone { get; set; } = "";
    
    [Column("dueOn")]
    public DateTime? DueOn { get; set; }
    
    [Required]
    [Column("conversation_id")]
    public long ConversationId { get; set; }
    public Conversation Conversation { get; internal set; }
    public ICollection<TaskEvent> TaskEvents { get; set; } = new List<TaskEvent>();
    
    public ICollection<TaskJob> TaskJobs { get; private set; } = new List<TaskJob>();
    
    /*
     * JSON Fields
     */
    
    [Column("repeater_json")]
    public string? RepeaterJson { get; private set; }

    [NotMapped]
    public TaskRepeater? Repeater
    {
        get => this.GetValueFromJson(() => (RepeaterJson, _repeater));
        set => this.SetJsonFieldFromValue(() => (RepeaterJson, _repeater = value), 
            newJson => RepeaterJson = newJson);
    }
    
    [Column("badger_json")]
    public string? BadgerJson { get; private set; }

    [NotMapped]
    public TaskBadger? Badger
    {
        get => this.GetValueFromJson(() => (BadgerJson, _badger));
        set => this.SetJsonFieldFromValue(() => (BadgerJson, _badger = value),
            newJson => BadgerJson = newJson);
    }
    
    [Required]
    [Column("completed_messages")]
    public string? CompletedMessagesJson { get; set; }

    [NotMapped]
    public List<string> CompletedMessages
    {
        get => this.GetListFromJsonField(() => (CompletedMessagesJson, _completedMessages));
        set => this.SetJsonFieldFromList(() => (CompletedMessagesJson, _completedMessages = value),
            newJson => CompletedMessagesJson = newJson);
    }
    
    [Column("skipped_messages")]
    public string? SkippedMessagesJson { get; set; }
    
    [NotMapped]
    public List<string> SkippedMessages
    {
        get => this.GetListFromJsonField(() => (SkippedMessagesJson, _skippedMessages));
        set => this.SetJsonFieldFromList(() => (SkippedMessagesJson, _skippedMessages = value),
            newJson => SkippedMessagesJson = newJson);
    }

    public List<DomainEvent> DomainEvents { get; set; } = [];
    
    public string GetRandomCompletedMessage()
    {
        if (CompletedMessages.Count == 0)
        {
            return $"Task {Name} completed!";
        }

        var random = new Random();
        return CompletedMessages[random.Next(CompletedMessages.Count)];
    }
    
    public string GetRandomSkippedMessage()
    {
        if (!CanBeSkipped)
        {
            return $"Task {Name} cannot be skipped!";
        }
        
        if (SkippedMessages.Count == 0)
        {
            return $"Task {Name} skipped!";
        }

        var random = new Random();
        return SkippedMessages[random.Next(SkippedMessages.Count)];
    }

    private MoneoTask() { }
    
    public MoneoTask(string name, string timezone, Conversation conversation)
    {
        Name = name;
        Timezone = timezone;
        Conversation = conversation;
    }

    public MoneoTaskDto ToDto()
    {
        return new MoneoTaskDto
        {
            Id = Id,
            Name = Name,
            Description = Description,
            DueOn = DueOn,
            CanBeSkipped = CanBeSkipped,
            Timezone = Timezone,
            IsActive = IsActive,
            CompletedMessages = CompletedMessages,
            SkippedMessages = SkippedMessages,
            Repeater = Repeater?.ToDto(),
            Badger = Badger?.ToDto(),
        };
    }
}
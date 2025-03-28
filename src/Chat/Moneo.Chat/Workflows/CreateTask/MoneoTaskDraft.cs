using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.Chat.Workflows.CreateTask;

public class MoneoTaskDraft
{
    private TaskRepeaterDraft? _repeater;
    private TaskBadgerDraft? _badger;
    private readonly bool _isForCreate;

    public bool IsRepeaterEnabled => _repeater is not null;

    public bool IsBadgerEnabled => _badger is not null;

    public MoneoTaskDto Task { get; set; } = new();
    
    public TaskRepeaterDraft? Repeater 
    {
        get => _repeater;
        set
        {
            if (value is not null)
            {
                EnableRepeater();
            }
            else
            {
                DisableRepeater();
            }
            _repeater = value;
        }
    }

    public void EnableRepeater()
    {
        _repeater = new TaskRepeaterDraft();
    }
    
    public void DisableRepeater()
    {
        _repeater = null;
    }
    
    public TaskBadgerDraft? Badger
    {
        get => _badger;
        set
        {
            if (value is not null)
            {
                EnableBadger();
            }
            else
            {
                DisableBadger();
            }
            _badger = value;
        }
    }

    public void EnableBadger()
    {
        _badger = new TaskBadgerDraft();
    }

    public void DisableBadger()
    {
        _badger = null;
    }
    
    public CreateEditTaskDto ToEditDto()
    {
        if (Task.CompletedMessages.Count == 0)
        {
            Task.CompletedMessages =
            [
                "Good job",
                "Well done",
                $"Finished {Task.Name}",
                $"{Task.Name} completed",
            ];
        }
        
        var dto = new CreateEditTaskDto(
            Task.Name,
            Task.Description!,
            _isForCreate || Task.IsActive,
            Task.CompletedMessages.ToList(),
            Task.CanBeSkipped,
            Task.SkippedMessages.ToList(),
            Task.Timezone,
            Task.DueOn,
            Repeater?.ToDto(),
            Badger?.ToDto());

        return dto;
    }

    public MoneoTaskDraft(bool isForCreate = false)
    {
        _isForCreate = isForCreate;
    }

    public MoneoTaskDraft(MoneoTaskDto dto)
    {
        Task = dto;
        if (dto.Repeater is not null)
        {
            EnableRepeater();
            _repeater = new TaskRepeaterDraft(dto.Repeater);
        }
        
        if (dto.Badger is not null)
        {
            EnableBadger();
            _badger = new TaskBadgerDraft(dto.Badger);
        }
    }
}

public class TaskRepeaterDraft
{
    public string CronExpression { get; set; } = string.Empty;
    public int EarlyCompletionThresholdHours { get; set; } = 3;
    public DateTime? Expiry { get; set; }

    public TaskRepeaterDraft()
    {
    }

    public TaskRepeaterDraft(TaskRepeaterDto dto)
    {
        CronExpression = dto.CronExpression;
        EarlyCompletionThresholdHours = dto.EarlyCompletionThresholdHours;
        Expiry = dto.Expiry?.UtcDateTime;
    }

    public TaskRepeaterDto ToDto()
    {
        return new TaskRepeaterDto(CronExpression, EarlyCompletionThresholdHours, Expiry);
    }
}

public class TaskBadgerDraft
{
    public int BadgerFrequencyInMinutes { get; set; }
    public List<string> BadgerMessages { get; set; } = [];
    
    public TaskBadgerDraft()
    {
    }
    
    public TaskBadgerDraft(TaskBadgerDto dto)
    {
        BadgerFrequencyInMinutes = dto.BadgerFrequencyInMinutes;
        BadgerMessages = dto.BadgerMessages.ToList();
    }
    
    public TaskBadgerDto ToDto()
    {
        return new TaskBadgerDto(BadgerFrequencyInMinutes, BadgerMessages);
    }
}

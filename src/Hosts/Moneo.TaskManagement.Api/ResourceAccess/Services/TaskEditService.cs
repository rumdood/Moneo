using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.ResourceAccess;

internal class TaskEditService : ITaskEditService
{
    private readonly MoneoTasksDbContext _dbContext;
    private readonly ITaskQueryService _taskQueryService;

    public TaskEditService(MoneoTasksDbContext dbContext, ITaskQueryService taskQueryService)
    {
        _dbContext = dbContext;
        _taskQueryService = taskQueryService;
    }
    
    public async Task<MoneoTaskDto> CreateTaskForConversationAsync(long conversationId, CreateTaskDto taskDto)
    {
        var conversation = await _dbContext.Conversations
            .Where(c => c.Id == conversationId)
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            throw new ArgumentException("Conversation not found", nameof(conversationId));
        }

        var taskExists = await DoesTaskExistAsync(conversationId, taskDto.Name);

        if (taskExists)
        {
            throw new InvalidOperationException(
                $"A task with the name '{taskDto.Name}' already exists in this conversation.");
        }

        var task = new MoneoTask
        {
            Name = taskDto.Name,
            Description = taskDto.Description,
            IsActive = taskDto.IsActive,
            CompletedMessages = string.Join(",", taskDto.CompletedMessages),
            CanBeSkipped = taskDto.CanBeSkipped,
            SkippedMessages = string.Join(",", taskDto.SkippedMessages),
            Timezone = taskDto.Timezone,
            DueOn = taskDto.DueOn,
            BadgerFrequencyInMinutes = taskDto.BadgerFrequencyInMinutes,
            BadgerMessages = taskDto.BadgerMessages,
            ConversationId = conversationId,
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow
        };

        if (taskDto.Repeater != null)
        {
            var repeater = new TaskRepeater
            {
                Expiry = taskDto.Repeater.Expiry,
                RepeatCron = taskDto.Repeater.RepeatCron,
                EarlyCompletionThresholdHours = taskDto.Repeater.EarlyCompletionThresholdHours,
                Task = task
            };
            task.TaskRepeater = repeater;
        }

        var entry = await _dbContext.MoneoTasks.AddAsync(task);
        await _dbContext.SaveChangesAsync();

        return entry.Entity.ToDto();
    }

    public async Task<MoneoTaskDto> UpdateTaskAsync(long taskId, UpdateTaskDto taskDto)
    {
        var task = await _dbContext.MoneoTasks
            .Where(t => t.Id == taskId)
            .Include(moneoTask => moneoTask.TaskRepeater)
            .FirstOrDefaultAsync();

        if (task == null)
        {
            throw new ArgumentException("Task not found", nameof(taskId));
        }

        return await UpdateTaskInternalAsync(task, taskDto);
    }

    public async Task<Result<MoneoTaskDto>> TryUpdateTaskAsync(TaskFilter filter, UpdateTaskDto taskDto)
    {
        var task = await GetSingleTaskAsync(filter);

        if (task is null)
        {
            return Result<MoneoTaskDto>.Failure("Task not found");
        }
        
        try
        {
            var updatedTask = await UpdateTaskInternalAsync(task, taskDto);
            return Result<MoneoTaskDto>.Success(updatedTask);
        }
        catch (Exception e)
        {
            return Result<MoneoTaskDto>.Failure(e);
        }
    }

    public async Task<Result> DeleteTaskAsync(long taskId) 
        => await TryDeleteTaskAsync(TaskFilter.ForTask(taskId));

    public async Task<Result> TryDeleteTaskAsync(TaskFilter filter)
    {
        var task = await GetSingleTaskAsync(filter);

        if (task is null)
        {
            return Result.Failure("Task not found");
        }

        try
        {
            _dbContext.MoneoTasks.Remove(task);
            await _dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(e);
        }
    }

    public async Task CompleteTaskAsync(long taskId)
    {
        var result = await TryCompleteTaskAsync(TaskFilter.ForTask(taskId));
        
        if (result.Exception != null)
        {
            throw result.Exception;
        }
        
        if (result.IsSuccess == false)
        {
            throw new InvalidOperationException(result.Error);
        }
    }

    public Task<Result> TryCompleteTaskAsync(TaskFilter filter)
        => TryCreateTaskEventAsync(filter, TaskEventType.Completed);

    public async Task SkipTaskAsync(long taskId)
    {
        var result = await TrySkipTaskAsync(TaskFilter.ForTask(taskId));
        
        if (result.Exception != null)
        {
            throw result.Exception;
        }
        
        if (result.IsSuccess == false)
        {
            throw new InvalidOperationException(result.Error);
        }
    }

    public Task<Result> TrySkipTaskAsync(TaskFilter filter)
        => TryCreateTaskEventAsync(filter, TaskEventType.Skipped);

    public async Task DeactivateTaskAsync(long taskId)
    {
        var result = await TryDeactivateTaskAsync(TaskFilter.ForTask(taskId));
        
        if (result.Exception != null)
        {
            throw result.Exception;
        }
        
        if (result.IsSuccess == false)
        {
            throw new InvalidOperationException(result.Error);
        }
    }

    public async Task<Result> TryDeactivateTaskAsync(TaskFilter filter)
    {
        var task = await GetSingleTaskAsync(filter);

        if (task is null)
        {
            return Result.Failure("Task not found");
        }

        if (task.IsActive == false)
        {
            return Result.Failure("Task is already deactivated");
        }

        task.IsActive = false;
        
        var completionResult = await TryCreateTaskEventAsync(task, TaskEventType.Disabled);
        
        if (completionResult.IsSuccess == false)
        {
            return completionResult;
        }
        
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }
        
    
    private async Task<bool> DoesTaskExistAsync(long conversationId, string name)
    {
        return await _dbContext.MoneoTasks
            .AsNoTracking()
            .AnyAsync(t => t.ConversationId == conversationId && t.Name == name);
    }

    private async Task<MoneoTask?> GetSingleTaskAsync(TaskFilter filter)
    {
        return await _dbContext.MoneoTasks
            .Where(filter.ToExpression())
            .Include(moneoTask => moneoTask.TaskRepeater)
            .SingleOrDefaultAsync();
    }
    
    private async Task<MoneoTask?> GetSingleTaskWithCompletionAsync(TaskFilter filter)
    {
        var query = _dbContext.MoneoTasks
            .Where(filter.ToExpression())
            .Include(moneoTask => moneoTask.TaskRepeater)
            .Select(task => new
            {
                Task = task,
                LastCompletedEvent = task.TaskEvents
                    .Where(te => te.Type == TaskEventType.Completed || te.Type == TaskEventType.Skipped)
                    .OrderByDescending(te => te.Timestamp)
                    .FirstOrDefault()
            });

        var result = await query.SingleOrDefaultAsync();

        if (result is null)
        {
            return null;
        }
        
        result.Task.TaskEvents = result.LastCompletedEvent is null
            ? new List<TaskEvent>()
            : new List<TaskEvent> { result.LastCompletedEvent };

        return result.Task;
    }
    
    private async Task<MoneoTaskDto> UpdateTaskInternalAsync(MoneoTask task, UpdateTaskDto taskDto)
    {
        task.Name = taskDto.Name;
        task.Description = taskDto.Description;
        task.IsActive = taskDto.IsActive;
        task.CompletedMessages = string.Join(",", taskDto.CompletedMessages);
        task.CanBeSkipped = taskDto.CanBeSkipped;
        task.SkippedMessages = string.Join(",", taskDto.SkippedMessages);
        task.Timezone = taskDto.Timezone;
        task.DueOn = taskDto.DueOn;
        task.ModifiedOn = DateTime.UtcNow;

        if (taskDto.Repeater != null)
        {
            if (task.TaskRepeater == null)
            {
                var repeater = new TaskRepeater
                {
                    Expiry = taskDto.Repeater.Expiry,
                    RepeatCron = taskDto.Repeater.RepeatCron,
                    EarlyCompletionThresholdHours = taskDto.Repeater.EarlyCompletionThresholdHours,
                    Task = task
                };
                task.TaskRepeater = repeater;
            }
            else
            {
                task.TaskRepeater.Expiry = taskDto.Repeater.Expiry;
                task.TaskRepeater.RepeatCron = taskDto.Repeater.RepeatCron;
                task.TaskRepeater.EarlyCompletionThresholdHours = taskDto.Repeater.EarlyCompletionThresholdHours;
            }
        }
        else
        {
            if (task.TaskRepeater != null)
            {
                _dbContext.TaskRepeaters.Remove(task.TaskRepeater);
                task.TaskRepeater = null;
            }
        }

        await _dbContext.SaveChangesAsync();

        return task.ToDto();
    }

    private async Task<Result> TryCreateTaskEventAsync(TaskFilter filter, TaskEventType eventType)
    {
        var task = await GetSingleTaskAsync(filter);
        
        if (task is null)
        {
            return Result.Failure("Task not found");
        }
        
        return await TryCreateTaskEventAsync(task, eventType);
    }

    private async Task<Result> TryCreateTaskEventAsync(MoneoTask task, TaskEventType eventType)
    {
        try
        {
            var completionEvent = new TaskEvent
            {
                Timestamp = DateTime.UtcNow,
                Type = eventType,
                Task = task
            };
            await _dbContext.TaskEvents.AddAsync(completionEvent);
            await _dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(e);
        }
    }
}
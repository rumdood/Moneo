using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.Features.CreateEditTask;

public record CreateEditTaskRequest(CreateEditTaskDto EditDto, long? TaskId = null, long? ConversationId = null) : IRequest<MoneoResult<long>>;

public sealed class CreateEditTaskHandler : IRequestHandler<CreateEditTaskRequest, MoneoResult<long>>
{
    private readonly MoneoTasksDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    
    public CreateEditTaskHandler(MoneoTasksDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<MoneoResult<long>> Handle(CreateEditTaskRequest request, CancellationToken cancellationToken)
    {
        MoneoTask? task;
        var isCreate = !request.TaskId.HasValue;
        
        if (!request.TaskId.HasValue)
        {
            if (!request.ConversationId.HasValue)
            {
                return MoneoResult<long>.BadRequest("Either TaskId or ConversationId must be provided");
            }
            
            var conversation = await _dbContext.Conversations.FindAsync(request.ConversationId, cancellationToken);
            
            if (conversation == null)
            {
                return MoneoResult<long>.ConversationNotFound("Conversation not found");
            }
            
            // check to see if a task with that name already exists
            var existing = await _dbContext.Tasks
                .AsNoTracking()
                .AnyAsync(t => t.ConversationId == request.ConversationId && t.Name == request.EditDto.Name,
                    cancellationToken);

            if (existing)
            {
                return MoneoResult<long>.AlreadyExists("A task with that name already exists");
            }

            task = new MoneoTask(request.EditDto.Name, request.EditDto.Timezone, conversation);
            await _dbContext.Tasks.AddAsync(task, cancellationToken);
        }
        else
        {
            task = await _dbContext.Tasks.FindAsync(request.TaskId.Value, cancellationToken);
            if (task is null)
            {
                return MoneoResult<long>.TaskNotFound("Task not found");
            }
        }

        task.ConversationId = request.ConversationId ?? task.ConversationId;
        task.Name = request.EditDto.Name;
        task.Description = request.EditDto.Description;
        task.DueOn = request.EditDto.DueOn;
        task.CanBeSkipped = request.EditDto.CanBeSkipped;
        task.Timezone = request.EditDto.Timezone;
        task.IsActive = request.EditDto.IsActive;
        task.Badger = request.EditDto.Badger is not null ? TaskBadger.FromDto(request.EditDto.Badger) : null;
        task.Repeater = request.EditDto.Repeater is not null ? TaskRepeater.FromDto(request.EditDto.Repeater) : null;

        var taskEvent = new TaskEvent(
            task, 
            isCreate ? TaskEventType.Created : TaskEventType.Updated,
            _timeProvider.GetUtcNow());
        await _dbContext.TaskEvents.AddAsync(taskEvent, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MoneoResult<long>.Success(task.Id);
    }
}

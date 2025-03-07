using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;
using TaskEvent = Moneo.TaskManagement.ResourceAccess.Entities.TaskEvent;

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
        
        if (isCreate)
        {
            if (request.ConversationId is null or 0)
            {
                return MoneoResult<long>.BadRequest("Either TaskId or ConversationId must be provided");
            }

            var conversation = await _dbContext.Conversations
                .Where(c => c.Id == request.ConversationId)
                .SingleOrDefaultAsync(cancellationToken);

            if (conversation == null)
            {
                // conversation isn't found. Later on maybe we'll toss those, but for now, create it.
                conversation = new Conversation(request.ConversationId.Value, Transport.Telegram); // everything is telegram right now
                await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
            }
            else
            {
                _dbContext.Conversations.Attach(conversation);
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
            task = await _dbContext.Tasks
                .Where(t => t.Id == request.TaskId)
                .SingleOrDefaultAsync(cancellationToken);
            if (task is null)
            {
                return MoneoResult<long>.TaskNotFound("Task not found");
            }
        }

        task.Name = request.EditDto.Name;
        task.Description = request.EditDto.Description;
        task.DueOn = request.EditDto.DueOn?.UtcDateTime;
        task.CanBeSkipped = request.EditDto.CanBeSkipped;
        task.Timezone =  request.EditDto.Timezone;
        task.IsActive = request.EditDto.IsActive;
        task.CompletedMessages = request.EditDto.CompletedMessages;
        task.SkippedMessages = request.EditDto.CanBeSkipped ? request.EditDto.SkippedMessages : [];
        task.Badger = request.EditDto.Badger is not null ? TaskBadger.FromDto(request.EditDto.Badger) : null;
        task.Repeater = request.EditDto.Repeater is not null ? TaskRepeater.FromDto(request.EditDto.Repeater) : null;

        var taskEvent = new TaskEvent(
            task, 
            isCreate ? TaskEventType.Created : TaskEventType.Updated,
            _timeProvider.GetUtcNow().UtcDateTime);
        await _dbContext.TaskEvents.AddAsync(taskEvent, cancellationToken);
        
        task.DomainEvents.Add(new TaskCreatedOrUpdated(_timeProvider.GetUtcNow().UtcDateTime, task));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MoneoResult<long>.Success(task.Id);
        }
        catch (Exception e)
        {
            return MoneoResult<long>.Failed(e);
        }
    }
}

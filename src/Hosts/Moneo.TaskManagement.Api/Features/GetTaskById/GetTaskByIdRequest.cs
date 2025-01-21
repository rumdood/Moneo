using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.TaskManagement.Features.GetTaskById;

public sealed record GetTaskByIdRequest(long TaskId) : IRequest<MoneoResult<MoneoTaskWithCompletionDataDto>>;

internal sealed class GetTaskByIdRequestHandler(MoneoTasksDbContext dbContext)
    : IRequestHandler<GetTaskByIdRequest, MoneoResult<MoneoTaskWithCompletionDataDto>>
{   
    public async Task<MoneoResult<MoneoTaskWithCompletionDataDto>> Handle(GetTaskByIdRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.Id == request.TaskId)
            .Select(t => new MoneoTaskWithCompletionDataDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                DueOn = t.DueOn,
                CanBeSkipped = t.CanBeSkipped,
                Timezone = t.Timezone,
                IsActive = t.IsActive,
                Badger = t.Badger != null ? t.Badger.ToDto() : null,
                Repeater = t.Repeater != null ? t.Repeater.ToDto() : null,
                LastCompleted = t.TaskEvents
                    .Where(e => e.Type == TaskEventType.Completed)
                    .OrderByDescending(e => e.OccurredOn)
                    .Select(e => e.OccurredOn)
                    .FirstOrDefault(),
                LastSkipped = t.TaskEvents
                    .Where(e => e.Type == TaskEventType.Skipped)
                    .OrderByDescending(e => e.OccurredOn)
                    .Select(e => e.OccurredOn)
                    .FirstOrDefault()
                
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return MoneoResult<MoneoTaskWithCompletionDataDto>.TaskNotFound("Task not found");
        }
        
        return MoneoResult<MoneoTaskWithCompletionDataDto>.Success(task);
    }
}

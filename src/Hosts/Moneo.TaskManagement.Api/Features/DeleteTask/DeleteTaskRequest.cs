using MediatR;
using Moneo.Common;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.TaskManagement.Features.DeleteTask;

public sealed record DeleteTaskRequest(long TaskId) : IRequest<MoneoResult>;

internal sealed class DeleteTaskRequestHandler : IRequestHandler<DeleteTaskRequest, MoneoResult>
{
    private readonly MoneoTasksDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    
    public DeleteTaskRequestHandler(MoneoTasksDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }
    
    public async Task<MoneoResult> Handle(DeleteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FindAsync([request.TaskId], cancellationToken: cancellationToken);
        if (task is null)
        {
            return MoneoResult.NoChange();
        }
        
        _dbContext.Tasks.Remove(task);
        task.DomainEvents.Add(new TaskDeleted(_timeProvider.GetUtcNow().UtcDateTime, task));
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MoneoResult.Success();
    }
}

using MediatR;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.TaskManagement.Features.DeleteTask;

public sealed record DeleteTaskRequest(long TaskId) : IRequest<MoneoResult>;

internal sealed class DeleteTaskRequestHandler(MoneoTasksDbContext dbContext)
    : IRequestHandler<DeleteTaskRequest, MoneoResult>
{
    public async Task<MoneoResult> Handle(DeleteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FindAsync([request.TaskId], cancellationToken: cancellationToken);
        if (task is null)
        {
            return MoneoResult.NoChange();
        }
        
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MoneoResult.Success();
    }
}

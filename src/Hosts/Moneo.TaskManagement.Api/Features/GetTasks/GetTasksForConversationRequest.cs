using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.TaskManagement.Features.GetTasks;

public sealed record GetTasksForConversationRequest(long ConversationId, PageOptions PagingOptions)
    : IRequest<MoneoResult<PagedList<MoneoTaskDto>>>;

internal sealed class GetTasksForConversationRequestHandler(MoneoTasksDbContext dbContext)
    : IRequestHandler<GetTasksForConversationRequest, MoneoResult<PagedList<MoneoTaskDto>>>
{
    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> Handle(GetTasksForConversationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var query = dbContext.Tasks
                .AsNoTracking()
                .Where(t => t.ConversationId == request.ConversationId);

            var totalCount = await query.CountAsync(cancellationToken);

            var tasks = await query
                .Skip(request.PagingOptions.PageNumber * request.PagingOptions.PageSize)
                .Take(request.PagingOptions.PageSize)
                .Select(t => t.ToDto())
                .ToListAsync(cancellationToken);

            var pagedList = new PagedList<MoneoTaskDto>
            {
                Data = tasks,
                Page = request.PagingOptions.PageNumber,
                PageSize = request.PagingOptions.PageSize,
                TotalCount = totalCount
            };

            return MoneoResult<PagedList<MoneoTaskDto>>.Success(pagedList);
        }
        catch (Exception e)
        {
            return MoneoResult<PagedList<MoneoTaskDto>>.Failed(e);
        }
    }
}
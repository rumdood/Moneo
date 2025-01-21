using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement.Features.GetTasks;

public sealed record GetTasksByFilterRequest(TaskFilter Filter, PageOptions PagingOptions)
    : IRequest<MoneoResult<PagedList<MoneoTaskDto>>>;

internal sealed class GetTasksByFilterRequestHandler(MoneoTasksDbContext dbContext)
    : IRequestHandler<GetTasksByFilterRequest, MoneoResult<PagedList<MoneoTaskDto>>>
{
    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> Handle(GetTasksByFilterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var query = dbContext.Tasks
                .AsNoTracking()
                .Where(request.Filter.ToExpression());

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

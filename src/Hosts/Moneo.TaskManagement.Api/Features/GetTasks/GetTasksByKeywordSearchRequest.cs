using FuzzySharp;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.Common;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.ResourceAccess;

namespace Moneo.TaskManagement.Api.Features.GetTasks;

public record GetTasksByKeywordSearchRequest(long ConversationId, string Keywords) : IRequest<MoneoResult<PagedList<MoneoTaskDto>>>;

internal sealed class GetTasksByKeywordSearchRequestHandler
    : IRequestHandler<GetTasksByKeywordSearchRequest, MoneoResult<PagedList<MoneoTaskDto>>>
{
    private const int MinMatchScore = 75;
    private readonly MoneoTasksDbContext _dbContext;

    public GetTasksByKeywordSearchRequestHandler(MoneoTasksDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> Handle(GetTasksByKeywordSearchRequest request, CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.ConversationId == request.ConversationId)
            .Select(t => t.ToDto())
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
        {
            return MoneoResult<PagedList<MoneoTaskDto>>.TaskNotFound(
                $"No tasks found for conversation {request.ConversationId}");
        }

        var matches = tasks.Select(task =>
            {
                var nameScore = Fuzz.PartialRatio(request.Keywords, task.Name);
                var descriptionScore = Fuzz.PartialRatio(request.Keywords, task.Description);
                var overallScore = Math.Max(nameScore, descriptionScore);
                return (task, overallScore);
            })
            .Where(match => match.overallScore >= MinMatchScore)
            .OrderByDescending(match => match.overallScore)
            .Select(match => match.task)
            .ToList();

        if (matches.Count == 0)
        {
            return MoneoResult<PagedList<MoneoTaskDto>>.TaskNotFound("No tasks found matching the search criteria");
        }
        
        var pagedList = new PagedList<MoneoTaskDto>
        {
            Data = matches,
            Page = 0,
            PageSize = matches.Count,
            TotalCount = matches.Count
        };
        
        return MoneoResult<PagedList<MoneoTaskDto>>.Success(pagedList);
    }
}

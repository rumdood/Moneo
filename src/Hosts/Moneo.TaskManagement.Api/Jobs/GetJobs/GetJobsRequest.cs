using MediatR;
using Moneo.Common;
using Moneo.TaskManagement.Scheduling;
using Quartz;
using Quartz.Impl.Matchers;

namespace Moneo.TaskManagement.Jobs.GetJobs;

public sealed record GetJobsRequest(PageOptions PageOptions) : IRequest<MoneoResult<PagedList<JobDto>>>;

internal sealed class GetJobsRequestHandler : IRequestHandler<GetJobsRequest, MoneoResult<PagedList<JobDto>>>
{
    private readonly ISchedulerService _schedulerService;

    public GetJobsRequestHandler(ISchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    public async Task<MoneoResult<PagedList<JobDto>>> Handle(GetJobsRequest request,
        CancellationToken cancellationToken)
    {
        var scheduler = _schedulerService.GetScheduler();

        if (scheduler is null)
        {
            return MoneoResult<PagedList<JobDto>>.Failed("Scheduler is not available");
        }

        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        
        var jobsForPage = jobKeys.Skip(request.PageOptions.PageSize * request.PageOptions.PageNumber)
            .Take(request.PageOptions.PageSize)
            .ToArray();
         
        var jobs = new List<JobDto>();

        foreach (var jobKey in jobsForPage)
        {
            var jobDetail = await scheduler.GetJobDetail(jobKey, cancellationToken);
            if (jobDetail != null)
            {
                var dataMap = jobDetail.JobDataMap.IsEmpty
                    ? null
                    : jobDetail.JobDataMap.Keys.ToDictionary(k => k, v => jobDetail.JobDataMap.GetValueOrDefault(v));
                
                jobs.Add(new JobDto(
                    jobDetail.Key.Name,
                    jobDetail.Key.Group,
                    jobDetail.JobType.FullName,
                    dataMap,
                    jobDetail.Description));
            }
            else
            {
                jobs.Add(new JobDto(jobKey.Name, jobKey.Group));
            }
        }

        return MoneoResult<PagedList<JobDto>>.Success(new PagedList<JobDto>
            {
            Data = jobs,
            Page = request.PageOptions.PageNumber,
            PageSize = request.PageOptions.PageSize,
            TotalCount = jobKeys.Count
        });
    }
}

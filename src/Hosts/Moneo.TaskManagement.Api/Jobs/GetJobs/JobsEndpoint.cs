using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moneo.Common;
using Moneo.Web;

namespace Moneo.TaskManagement.Jobs.GetJobs;

public static class JobsEndpoint
{
    public static RouteHandlerBuilder AddGetJobsEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/api/jobs", async (HttpContext context, ISender sender) =>
        {
            var queryString = context.Request.QueryString.Value;
            
            if (!PageOptions.TryParse(queryString, provider: null, out var pageOptions))
            {
                pageOptions = new PageOptions(1, 100);
            }
            
            var jobs = await sender.Send(new GetJobsRequest(pageOptions));
            return jobs.GetHttpResult();
        });
    }
}
using MediatR;
using Moneo.Common;
using Moneo.TaskManagement.Api.Features.GetTasks;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.Web;

namespace Moneo.TaskManagement.Features.GetTasks;

public static class GetTaskEndpoints
{
    public static RouteHandlerBuilder AddGetTaskByFilterEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/api/tasks",
            async (HttpContext context, ISender sender) =>
            {
                var queryString = context.Request.QueryString.Value;
            
                if (!PageOptions.TryParse(queryString, provider: null, out var pagingOptions))
                {
                    pagingOptions = new PageOptions(1, 100);
                }
                
                if (!TaskFilter.TryParse(queryString, provider: null, out var filter))
                {
                    filter = new TaskFilter();
                }
                
                var result = await sender.Send(new GetTasksByFilterRequest(filter, pagingOptions));
                return result.GetHttpResult();
            });
    }

    public static RouteHandlerBuilder AddGetTasksForConversationEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/api/conversations/{conversationId:long}/tasks",
            async (long conversationId, HttpContext context, ISender sender) =>
            {
                var queryString = context.Request.QueryString.Value;
            
                if (!PageOptions.TryParse(queryString, provider: null, out var pagingOptions))
                {
                    pagingOptions = new PageOptions(1, 100);
                }
                
                var result = await sender.Send(new GetTasksForConversationRequest(conversationId, pagingOptions));
                return result.GetHttpResult();
            });
    }
    
    public static RouteHandlerBuilder AddGetTasksByKeywordEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/api/conversations/{conversationId:long}/tasks/search",
            async (long conversationId, HttpContext context, ISender sender,
                CancellationToken cancellationToken = default) =>
            {
                var keyword = context.Request.Query["keyword"];

                if (string.IsNullOrEmpty(keyword))
                {
                    return Results.BadRequest("Keyword is required.");
                }
                
                var result = await sender.Send(
                    new GetTasksByKeywordSearchRequest(conversationId, keyword!),
                    cancellationToken);
                
                return result.GetHttpResult();
            });
    }
}
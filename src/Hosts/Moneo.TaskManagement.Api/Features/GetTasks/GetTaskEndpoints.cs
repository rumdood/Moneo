using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moneo.Common;
using Moneo.TaskManagement.Api.Features.GetTasks;
using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.TaskManagement.Features.GetTasks;

public static class GetTaskEndpoints
{
    public static void AddGetTaskByFilterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tasks",
            async ([FromQuery] TaskFilter filter, [FromQuery] PageOptions pagingOptions, ISender sender) =>
            {
                var result = await sender.Send(new GetTasksByFilterRequest(filter, pagingOptions));
                return result.GetHttpResult();
            });
    }

    public static void AddGetTasksForConversationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/conversations/{conversationId:long}/tasks",
            async (long conversationId, [FromQuery] PageOptions pagingOptions, ISender sender) =>
            {
                var result = await sender.Send(new GetTasksForConversationRequest(conversationId, pagingOptions));
                return result.GetHttpResult();
            });
    }
    
    public static void AddGetTasksByKeywordEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/conversations/{conversationId:long}/tasks/search",
            async (long conversationId, [FromQuery] string keyword, ISender sender,
                CancellationToken cancellationToken = default) =>
            {
                var result = await sender.Send(
                    new GetTasksByKeywordSearchRequest(conversationId, keyword),
                    cancellationToken);
                
                return result.GetHttpResult();
            });
    }
}
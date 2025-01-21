using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.Features.DeactivateTask;

public static class DeactivateTaskEndpoint
{
    public static void AddDeactivateTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks/{taskId:long}/deactivate", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateTaskRequest(taskId));
            return result.GetHttpResult();
        });
    }
}
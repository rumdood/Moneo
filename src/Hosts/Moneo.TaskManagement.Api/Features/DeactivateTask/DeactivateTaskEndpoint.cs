using MediatR;

namespace Moneo.TaskManagement.Features.DeactivateTask;

public static class DeactivateTaskEndpoint
{
    public static RouteHandlerBuilder AddDeactivateTaskEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/api/tasks/{taskId:long}/deactivate", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateTaskRequest(taskId));
            return result.GetHttpResult();
        });
    }
}
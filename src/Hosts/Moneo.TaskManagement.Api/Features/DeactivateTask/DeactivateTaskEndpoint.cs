using MediatR;

namespace Moneo.TaskManagement.Features.DeactivateTask;

public static class DeactivateTaskEndpoint
{
    public static void AddDeactivateTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:long}/deactivate", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateTaskRequest(taskId));
            return result.GetHttpResult();
        });
    }
}
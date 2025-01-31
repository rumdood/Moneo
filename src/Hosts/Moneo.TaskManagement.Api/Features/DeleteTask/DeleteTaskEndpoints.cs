using MediatR;

namespace Moneo.TaskManagement.Features.DeleteTask;

public static class DeleteTaskEndpoints
{
    public static void AddDeleteTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/tasks/{taskId:long}", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new DeleteTaskRequest(taskId));
            return result.GetHttpResult();
        });
    }
}

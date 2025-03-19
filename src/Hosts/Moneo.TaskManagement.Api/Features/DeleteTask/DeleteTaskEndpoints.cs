using MediatR;
using Moneo.Web;

namespace Moneo.TaskManagement.Features.DeleteTask;

public static class DeleteTaskEndpoints
{
    public static RouteHandlerBuilder AddDeleteTaskEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapDelete("/api/tasks/{taskId:long}", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new DeleteTaskRequest(taskId));
            return result.GetHttpResult();
        });
    }
}

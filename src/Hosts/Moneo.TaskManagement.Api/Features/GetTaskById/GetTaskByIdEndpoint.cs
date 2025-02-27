using MediatR;

namespace Moneo.TaskManagement.Features.GetTaskById;

public static class GetTaskByIdEndpoint
{
    public static RouteHandlerBuilder AddGetTaskByIdEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/api/tasks/{taskId:long}", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new GetTaskByIdRequest(taskId));
            return result.GetHttpResult();
        });
    }
}

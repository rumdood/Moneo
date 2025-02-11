using MediatR;

namespace Moneo.TaskManagement.Features.GetTaskById;

public static class GetTaskByIdEndpoint
{
    public static void AddGetTaskByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tasks/{taskId:long}", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new GetTaskByIdRequest(taskId));
            return result.GetHttpResult();
        });
    }
}

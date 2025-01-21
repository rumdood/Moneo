using MediatR;
using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.Features.GetTaskById;

public static class GetTaskByIdEndpoint
{
    public static void AddGetTaskByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tasks/{taskId:long}", async (long taskId, ISender sender) =>
        {
            var result = await sender.Send(new GetTaskByIdRequest(taskId));
            return result.GetHttpResult();
        });
    }
}

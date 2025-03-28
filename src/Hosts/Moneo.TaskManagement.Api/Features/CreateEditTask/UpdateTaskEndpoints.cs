using MediatR;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.Web;

namespace Moneo.TaskManagement.Features.CreateEditTask;

public static class UpdateTaskEndpoints
{
    public static RouteHandlerBuilder AddUpdateTaskEndpoints(this IEndpointRouteBuilder app)
    {
        return app.MapPut("/api/tasks/{taskId:long}", async (
            long taskId, 
            CreateEditTaskDto taskDto, 
            ISender sender,
            CancellationToken cancellationToken = default) =>
        {
            var request = new CreateEditTaskRequest(EditDto: taskDto, TaskId: taskId);
            var result = await sender.Send(request, cancellationToken);

            return result.GetHttpResult();
        });
    }
}
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.Features.CreateEditTask;

public static class UpdateTaskEndpoints
{
    public static void AddUpdateTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/tasks/{taskId:long}", async (long taskId, CreateEditTaskDto taskDto, ISender sender, CancellationToken cancellationToken = default) =>
        {
            var request = new CreateEditTaskRequest(EditDto: taskDto, TaskId: taskId);
            var result = await sender.Send(request, cancellationToken);

            return result.GetHttpResult();
        });
    }
}
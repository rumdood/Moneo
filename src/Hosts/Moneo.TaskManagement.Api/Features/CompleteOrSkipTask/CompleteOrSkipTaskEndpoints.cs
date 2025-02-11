using MediatR;

namespace Moneo.TaskManagement.Api.Features.CompleteTask;

public static class CompleteOrSkipTaskEndpoints
{
    public static void AddSkipTaskEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:long}/skip",
            async (long taskId, ISender sender) =>
            {
                var result = await sender.Send(new CompleteOrSkipTaskRequest(taskId, TaskCompletionType.Skipped));
                return result.GetHttpResult();
            });
    }
    
    public static void AddCompleteTaskEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:long}/complete",
            async (long taskId, ISender sender) =>
            {
                var result = await sender.Send(new CompleteOrSkipTaskRequest(taskId, TaskCompletionType.Completed));
                return result.GetHttpResult();
            });
    }
}
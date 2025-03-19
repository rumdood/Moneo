using MediatR;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.Web;

namespace Moneo.TaskManagement.Features.CreateEditTask;

public static class CreateTaskEndpoint
{
    public static RouteHandlerBuilder AddCreateTaskEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/api/conversations/{conversationId:long}/tasks",
            async (long conversationId, CreateEditTaskDto taskDto, ISender sender) =>
            {
                var result = await sender.Send(
                    new CreateEditTaskRequest(ConversationId: conversationId, EditDto:taskDto));
                return result.IsSuccess ? 
                    Results.Created($"/tasks/{result.Data}", result) : 
                    result.GetHttpResult();
            });
    }
}

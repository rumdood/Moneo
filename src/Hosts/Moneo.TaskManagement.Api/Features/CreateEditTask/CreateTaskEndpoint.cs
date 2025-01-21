using MediatR;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Model;

namespace Moneo.TaskManagement.Features.CreateEditTask;

public static class CreateTaskEndpoint
{
    public static void AddCreatTaskEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("{conversationId:long}/tasks",
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

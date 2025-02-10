using Moneo.TaskManagement.Api.Features.CompleteTask;
using Moneo.TaskManagement.Features.CreateEditTask;
using Moneo.TaskManagement.Features.DeactivateTask;
using Moneo.TaskManagement.Features.DeleteTask;
using Moneo.TaskManagement.Features.GetTaskById;
using Moneo.TaskManagement.Features.GetTasks;
using Moneo.TaskManagement.Jobs.GetJobs;

namespace Moneo.TaskManagement.Api.ServiceCollectionExtensions;

public static class AppBuilderExtensions
{
    public static void AddTaskManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/about", () => "Moneo Task Management API");
        app.AddCreatTaskEndpoint();
        app.AddUpdateTaskEndpoints();
        app.AddCompleteTaskEndpoint();
        app.AddSkipTaskEndpoint();
        app.AddDeactivateTaskEndpoints();
        app.AddDeleteTaskEndpoints();
        app.AddGetTaskByFilterEndpoint();
        app.AddGetTasksForConversationEndpoint();
        app.AddGetTaskByIdEndpoint();
        app.AddGetJobsEndpoint();
    }
}
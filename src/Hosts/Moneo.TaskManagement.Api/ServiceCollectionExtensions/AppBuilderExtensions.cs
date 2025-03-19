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
    public static void AddTaskManagementEndpoints(
        this IEndpointRouteBuilder app, 
        Action<TaskManagementEndpointOptions>? configureOptions = null)
    {
        var options = new TaskManagementEndpointOptions();
        configureOptions?.Invoke(options);
        app.AddTaskManagementEndpoints(options);
    }
    
    private static void AddTaskManagementEndpoints(this IEndpointRouteBuilder app, TaskManagementEndpointOptions? options)
    {
        app.MapGet("/api/about", () => "Moneo Task Management API");
        var createTaskEndpoint = app.AddCreateTaskEndpoint();
        var updateTaskEndpoint = app.AddUpdateTaskEndpoints();
        var completeTaskEndpoint = app.AddCompleteTaskEndpoint();
        var skipTaskEndpoint = app.AddSkipTaskEndpoint();
        var deactivateTaskEndpoints = app.AddDeactivateTaskEndpoints();
        var deleteTaskEndpoints = app.AddDeleteTaskEndpoints();
        var getTaskByFilterEndpoint = app.AddGetTaskByFilterEndpoint();
        var getTasksForConversationEndpoint = app.AddGetTasksForConversationEndpoint();
        var getTasksByKeywordEndpoint = app.AddGetTasksByKeywordEndpoint();
        var getTaskByIdEndpoint = app.AddGetTaskByIdEndpoint();
        var getJobsEndpoint = app.AddGetJobsEndpoint();
        
        if (string.IsNullOrEmpty(options?.AuthorizationPolicy))
        {
            return;
        }
        
        createTaskEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        updateTaskEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        completeTaskEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        skipTaskEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        deactivateTaskEndpoints.RequireAuthorization(options.AuthorizationPolicy);
        deleteTaskEndpoints.RequireAuthorization(options.AuthorizationPolicy);
        getTaskByFilterEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        getTasksForConversationEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        getTasksByKeywordEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        getTaskByIdEndpoint.RequireAuthorization(options.AuthorizationPolicy);
        getJobsEndpoint.RequireAuthorization(options.AuthorizationPolicy);
    }
}

public class TaskManagementEndpointOptions
{
    public string? AuthorizationPolicy { get; private set; }
    
    public void RequireAuthorization(string policy)
    {
        AuthorizationPolicy = policy;
    }
}
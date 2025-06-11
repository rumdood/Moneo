using Moneo.TaskManagement.Workflows;

namespace Moneo.Hosts.Chat.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagementChat(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowWithTaskDraftStateMachineRepository, TaskCreateOrChangeStateMachineRepository>();
        return services;
    }
}
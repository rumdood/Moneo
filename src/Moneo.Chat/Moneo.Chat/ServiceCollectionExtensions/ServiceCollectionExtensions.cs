using Microsoft.Extensions.DependencyInjection;
using Moneo.Chat.Workflows;
using Moneo.Chat.Workflows.CreateCronSchedule;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowManagers(this IServiceCollection services) =>
        services
            .AddSingleton<ICompleteTaskWorkflowManager, CompleteTaskWorkflowManager>()
            .AddSingleton<ICreateTaskWorkflowManager, CreateTaskWorkflowManager>()
            .AddSingleton<ICreateCronWorkflowManager, CreateCronWorkflowManager>()
            .AddSingleton<IConfirmCommandWorkflowManager, ConfirmCommandWorkflowManager>();
}
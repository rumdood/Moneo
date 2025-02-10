using Microsoft.Extensions.Options;
using Moneo.TaskManagement.Contracts;

namespace Moneo.Hosts.Chat.Api.Tasks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagement(
        this IServiceCollection services,
        Action<TaskManagementOptions> configure)
    {
        var options = new TaskManagementOptions();
        configure(options);
        
        services.AddSingleton(options);
        services.AddHttpClient<ITaskManagerClient, TasksClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("ApiKey", options.ApiKey);
        });

        return services;
    }
}

public class TaskManagementOptions
{
    public TaskManagementConfig Configuration { get; private set; }

    public string BaseUrl
    {
        get => Configuration.BaseUrl;
        set
        {
            if (Configuration is null)
            {
                Configuration = new TaskManagementConfig();
            }

            Configuration.BaseUrl = value;
        }
    }

    public string ApiKey
    {
        get => Configuration.ApiKey;
        set
        {
            if (Configuration is null)
            {
                Configuration = new TaskManagementConfig();
            }

            Configuration.ApiKey = value;
        }
    }

    public void UseConfiguration(TaskManagementConfig taskManagementConfig)
    {
        Configuration = taskManagementConfig;
    }
}

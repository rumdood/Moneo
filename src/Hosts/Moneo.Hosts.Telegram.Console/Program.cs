using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Moneo.Chat;
using Moneo.Chat.Telegram;
using Moneo.Common;
using Moneo.Hosts.Chat.Api;
using Moneo.TaskManagement.Contracts;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.Workflows.CreateTask;
using Moneo.Web;
using RadioFreeBot;
using RadioFreeBot.Configuration;
using RadioFreeBot.Features.FindSong;

var builder = Host.CreateDefaultBuilder(args);

// load the configuration from the appsettings.json, environment variables, and command line arguments
builder.ConfigureAppConfiguration((context, config) =>
{
    context.HostingEnvironment.ContentRootPath = AppContext.BaseDirectory;
    context.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    
    config.SetBasePath(AppContext.BaseDirectory);
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    config.AddJsonFile($"appsettings.local.json", optional: true, reloadOnChange: true);
    config.AddUserSecrets<Program>(optional: true);
    config.AddEnvironmentVariables();
    config.AddCommandLine(args);
});

builder.ConfigureServices((context, services) =>
{
    var configuration = context.Configuration;

    services.AddSerilog(
        opt => opt
            .WriteTo.Console()
            .ReadFrom.Configuration(configuration));

    var taskManagementConfig = configuration.GetSection("Moneo:TaskManagement").Get<TaskManagementConfig>();
    if (taskManagementConfig is null)
    {
        throw new InvalidOperationException("Moneo:TaskManagement configuration section is missing.");
    }

    services.AddSingleton(taskManagementConfig);
    
    var radioFreeBotConfiguration = configuration.GetSection("RadioFree").Get<RadioFreeBotConfiguration>();

    services.AddSingleton(radioFreeBotConfiguration!);
    services.AddSingleton(radioFreeBotConfiguration!.YouTubeMusicProxy);
    
    var chatConfig = configuration.GetSection("Moneo:Chat").Get<ChatConfig>();
    if (chatConfig is null)
    {
        throw new InvalidOperationException("Moneo:Chat configuration section is missing.");
    }
    
    services.AddSingleton(chatConfig);

    services.AddTelegramChatAdapter(opts =>
    {
        var masterConversationId = configuration.GetValue<long>("Telegram:MasterConversationId");
        var botToken = configuration.GetValue<string>("Telegram:BotToken");

        if (string.IsNullOrEmpty(botToken))
        {
            throw new InvalidOperationException("Telegram:BotToken configuration is missing.");
        }

        opts.MasterConversationId = masterConversationId;
        opts.BotToken = botToken;
        opts.UseInMemoryStateManagement();
        opts.RegisterAsHostedService();

        if (chatConfig.LoadTaskManagementCommands)
        {
            opts.RegisterUserRequestsAndWorkflowsFromAssemblyContaining<CreateTaskRequest>();
        }
        
        if (chatConfig.LoadRadioFreeBotCommands)
        {
            opts.RegisterUserRequestsAndWorkflowsFromAssemblyContaining<FindSongRequest>();
        }
    });

    services.AddTaskManagementChat();

    services.AddTaskManagement(opt =>
    {
        opt.UseConfiguration(taskManagementConfig);
    });

    services.AddPlaylistManagement(opt =>
    {
        opt.ConfigureYouTubeProxy(ytopt =>
        {
            ytopt.YouTubeMusicProxyUrl = radioFreeBotConfiguration.YouTubeMusicProxy.YouTubeMusicProxyUrl;
        });
        opt.UseSqliteDatabase(configuration.GetConnectionString("RadioFree")!);
    });
});

var app = builder.Build();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Moneo Chat Adapter started.");

    // Resolve IChatAdapter after the application has started
    var chatAdapter = app.Services.GetRequiredService<IChatAdapter>();
    logger.LogInformation("IChatAdapter resolved successfully.");
    
    // fire the RadioFreeBot.Events.ApplicationStartedEvent
    var mediator = app.Services.GetRequiredService<IMediator>();
    mediator.Publish(new RadioFreeBot.Events.ApplicationStartedEvent(DateTime.UtcNow)).GetAwaiter().GetResult();
    
});

lifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Moneo Chat Adapter stopping.");
});

await app.RunAsync();


public class TaskManagementConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class ChatConfig
{
    public string? PrivateKey { get; set; }
    public string DefaultTimezone { get; set; }
    public bool LoadTaskManagementCommands { get; set; } = false;
    public bool LoadRadioFreeBotCommands { get; set; } = false;
}

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
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
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

internal class TasksClient : ITaskManagerClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public TasksClient(HttpClient httpClient, TaskManagementConfig config)
    {
        _baseUrl = config.BaseUrl;
        _apiKey = config.ApiKey;
        _httpClient = httpClient;
    }

    private async Task<MoneoResult<T>> SendRequestAsync<T>(HttpMethod method, string uri, object? content = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(new Uri(_baseUrl), uri);
        var requestMessage = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(content)
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            requestMessage.Headers.Add("X-Api-Key", _apiKey);
        }

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var result = await response.GetMoneoResultAsync<T>(cancellationToken);
        return result;
    }
    
    public async Task<MoneoResult<MoneoTaskDto>> CreateTaskAsync(long conversationId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<MoneoTaskDto>(HttpMethod.Post, $"api/conversations/{conversationId}/tasks", dto, cancellationToken);
    }

    public async Task<MoneoResult> UpdateTaskAsync(long taskId, CreateEditTaskDto dto, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Put, $"api/tasks/{taskId}", dto, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }
    
    public async Task<MoneoResult> CompleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Post, $"api/tasks/{taskId}/complete", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult> SkipTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Post, $"api/tasks/{taskId}/skip", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForConversationAsync(long conversationId,
        PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        var query =
            $"api/conversations/{conversationId}/tasks?pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAsync(long userId, PageOptions pagingOptions,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/users/{userId}/tasks?pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksForUserAndConversationAsync(long userId,
        long conversationId, PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        var query =
            $"api/users/{userId}/conversations/{conversationId}/tasks?pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<PagedList<MoneoTaskDto>>> GetTasksByKeywordSearchAsync(long conversationId,
        string keyword, PageOptions pagingOptions, CancellationToken cancellationToken = default)
    {
        var query =
            $"api/conversations/{conversationId}/tasks/search?keyword={keyword}&pn={pagingOptions.PageNumber}&ps={pagingOptions.PageSize}";
        return await SendRequestAsync<PagedList<MoneoTaskDto>>(HttpMethod.Get, query, null, cancellationToken);
    }

    public async Task<MoneoResult<MoneoTaskDto>> GetTaskAsync(long taskId,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<MoneoTaskDto>(HttpMethod.Get, $"api/tasks/{taskId}", null, cancellationToken);
    }

    public async Task<MoneoResult> DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync<object>(HttpMethod.Delete, $"api/tasks/{taskId}", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }

    public async Task<MoneoResult> DeactivateTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var result =
            await SendRequestAsync<object>(HttpMethod.Post, $"api/tasks/{taskId}/deactivate", null, cancellationToken);
        return result.IsSuccess ? MoneoResult.Success() : MoneoResult.Failed(result.Message);
    }
}

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Chat;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Telegram;
using Moneo.Functions.Isolated;
using Moneo.Functions.Isolated.TaskManager;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Telegram.Bot;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Azure.Core.Serialization;
using Moneo.Chat.ServiceCollectionExtensions;

var builder = new HostBuilder();

var tgToken = Environment.GetEnvironmentVariable("telegramBotToken", EnvironmentVariableTarget.Process)
    ?? throw new ArgumentException("Can not get token. Set token in environment setting");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Moneo.Functions", LogEventLevel.Verbose)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Worker", LogEventLevel.Warning)
    .MinimumLevel.Override("Host", LogEventLevel.Warning)
    .MinimumLevel.Override("Function", LogEventLevel.Warning)
    .MinimumLevel.Override("Azure", LogEventLevel.Warning)
    .MinimumLevel.Override("DurableTask", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.ApplicationInsights(
        TelemetryConfiguration.CreateDefault(),
        TelemetryConverter.Events)
    .WriteTo.Console()
    .CreateLogger();

builder.ConfigureFunctionsWorkerDefaults(opts =>
    {
        opts.UseNewtonsoftJson();
    })
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(BotTextMessageRequest).Assembly,
                typeof(TelegramChatAdapter).Assembly);
        });
        serviceCollection.AddLogging(cfg =>
        {
            cfg.AddSerilog();
        });
        serviceCollection.AddMemoryCache();
        serviceCollection.AddBotConfiguration();
        serviceCollection.AddSingleton<ITaskResourceManager, TaskResourceManager>();
        serviceCollection.AddSingleton<ITaskManagerClient, TaskManagerHttpClient>();
        serviceCollection.AddSingleton<IMoneoTaskFactory, MoneoTaskFactory>();
        serviceCollection.AddSingleton<INotifyEngine, NotifyEngine>();
        serviceCollection.AddSingleton<IScheduleManager, ScheduleManager>();

        serviceCollection.AddSingleton<IChatManager, ChatManager>();
        serviceCollection.AddSingleton<IChatAdapter, TelegramChatAdapter>();
        serviceCollection.AddSingleton<IChatStateRepository, InMemoryChatStateRepository>();
        serviceCollection.AddWorkflowManagers();

        // Register named HttpClient to get benefits of IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        // More read:
        //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
        //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        serviceCollection.AddHttpClient("tgclient")
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(tgToken, httpClient));
    });

var host = builder.Build();

host.Run();


public static class  FunctionsWorkerApplicationBuilderExtensions
{
    public static IFunctionsWorkerApplicationBuilder UseNewtonsoftJson(this IFunctionsWorkerApplicationBuilder builder)
    {
        builder.Services.Configure<WorkerOptions>(workerOptions =>
        {
            var settings = NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.NullValueHandling = NullValueHandling.Ignore;

            workerOptions.Serializer = new NewtonsoftJsonObjectSerializer(settings);
        });

        return builder;
    }
}

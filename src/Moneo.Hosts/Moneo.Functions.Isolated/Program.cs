using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Chat;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Telegram;
using Moneo.Functions.Isolated;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client;
using Telegram.Bot;

var builder = new HostBuilder();

var tgToken = System.Environment.GetEnvironmentVariable("tgToken", EnvironmentVariableTarget.Process)
    ?? throw new ArgumentException("Can not get token. Set token in environment setting");

builder.ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(BotTextMessageRequest).Assembly,
                typeof(TelegramChatAdapter).Assembly);
        });
        serviceCollection.AddMemoryCache();
        serviceCollection.AddBotConfiguration();
        serviceCollection.AddSingleton<IChatManager, ChatManager>();
        serviceCollection.AddSingleton<ITaskResourceManager, TaskResourceManager>();
        serviceCollection.AddSingleton<IChatAdapter, TelegramChatAdapter>();
        
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

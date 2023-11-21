using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Chat;
using Moneo.Chat.UserRequests;
using Moneo.TaskManagement;

var builder = Host.CreateApplicationBuilder(args);

const string localJson = "appsettings.local.json";
if (File.Exists(localJson))
{
    builder.Configuration.AddJsonFile(localJson, optional: true, reloadOnChange: true);
    Console.WriteLine("Loaded {0}", localJson);
}

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(CompleteTaskRequest).Assembly, typeof(BotService).Assembly);
});
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IConversationManager, ConversationManager>();
builder.Services.AddSingleton<ITaskResourceManager, TaskResourceManager>();
builder.Services.AddSingleton<ITaskManagerClient, TaskManagerHttpClient>();
builder.Services.Configure<BotClientConfiguration>(builder.Configuration.GetSection(nameof(BotClientConfiguration)));
builder.Services.AddHostedService<BotService>();

var host = builder.Build();
await host.RunAsync();

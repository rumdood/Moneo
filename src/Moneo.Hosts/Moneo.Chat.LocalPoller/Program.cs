using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Chat;
using Moneo.Chat.UserRequests;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client;

var builder = Host.CreateApplicationBuilder(args);

const string localJson = "appsettings.local.json";
if (File.Exists(localJson))
{
    builder.Configuration.AddJsonFile(localJson, optional: true, reloadOnChange: true);
    Console.WriteLine("Loaded {0}", localJson);
}
else
{
    Console.WriteLine("I didn't find the local file");
    Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
    Console.ReadKey();
}

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(CompleteTaskRequest).Assembly, typeof(BotService).Assembly);
});
builder.Services.AddMemoryCache();

IBotClientConfiguration botConfig = new BotClientConfiguration();
builder.Configuration.GetSection(nameof(BotClientConfiguration)).Bind(botConfig);

builder.Services.AddScoped<IBotClientConfiguration>(_ => botConfig);

builder.Services.AddSingleton<IConversationManager, ConversationManager>();
builder.Services.AddSingleton<ITaskResourceManager, TaskResourceManager>();
builder.Services.AddSingleton<ITaskManagerClient, TaskManagerHttpClient>();

builder.Services.AddHostedService<BotService>();

var host = builder.Build();
await host.RunAsync();

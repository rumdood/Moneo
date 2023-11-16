using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Bot;
using Moneo.Bot.Commands;

var builder = Host.CreateApplicationBuilder(args);

const string localJson = "appsettings.local.json";
if (File.Exists(localJson))
{
    builder.Configuration.AddJsonFile(localJson, optional: true, reloadOnChange: true);
    Console.WriteLine("Loaded {0}", localJson);
}

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(IMoneoCommand).Assembly);
});
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IConversationManager, ConversationManager>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IMoneoProxy, MoneoProxy>();
builder.Services.Configure<BotClientConfiguration>(builder.Configuration.GetSection(nameof(BotClientConfiguration)));
builder.Services.AddHostedService<BotService>();

var host = builder.Build();
await host.RunAsync();

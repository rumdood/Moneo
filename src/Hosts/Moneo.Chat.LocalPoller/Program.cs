using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Chat;
using Moneo.Chat.ServiceCollectionExtensions;
using Moneo.Core;
using Moneo.Obsolete.TaskManagement;
using Moneo.Obsolete.TaskManagement.Client;

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine(AppContext.BaseDirectory);
builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory);

const string localJson = "appsettings.local.json";
builder.Configuration.AddJsonFile(localJson, optional: true, reloadOnChange: true);

IBotClientConfiguration botConfig = new BotClientConfiguration();
builder.Configuration.GetSection(nameof(BotClientConfiguration)).Bind(botConfig);

builder.Services.AddChatAdapter<ConsoleChatAdapter>();
builder.Services.AddInMemoryChatStateManagement();
builder.Services.AddChatManager();

builder.Services.AddMemoryCache();
builder.Services.AddScoped(_ => botConfig);
builder.Services.AddSingleton<ITaskResourceManager, TaskResourceManager>();
builder.Services.AddSingleton<ITaskManagerClient, TaskManagerHttpClient>();

builder.Services.AddWorkflowManagers();

builder.Services.AddHostedService<BotService>();

var host = builder.Build();

await host.RunAsync();

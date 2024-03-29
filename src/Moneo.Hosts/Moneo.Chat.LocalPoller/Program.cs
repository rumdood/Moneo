﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moneo.Chat;
using Moneo.Chat.ServiceCollectionExtensions;
using Moneo.Chat.Telegram;
using Moneo.Core;
using Moneo.TaskManagement;
using Moneo.TaskManagement.Client;

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine(AppContext.BaseDirectory);
builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory);

const string localJson = "appsettings.local.json";
builder.Configuration.AddJsonFile(localJson, optional: true, reloadOnChange: true);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(CompleteTaskRequest).Assembly, typeof(TelegramChatAdapter).Assembly);
});
builder.Services.AddMemoryCache();

IBotClientConfiguration botConfig = new BotClientConfiguration();
builder.Configuration.GetSection(nameof(BotClientConfiguration)).Bind(botConfig);

builder.Services.AddScoped(_ => botConfig);

builder.Services.AddSingleton<IChatManager, ChatManager>();
builder.Services.AddSingleton<ITaskResourceManager, TaskResourceManager>();
builder.Services.AddSingleton<ITaskManagerClient, TaskManagerHttpClient>();
builder.Services.AddSingleton<IChatStateRepository, InMemoryChatStateRepository>();
builder.Services.AddSingleton<IChatAdapter, TelegramChatAdapter>();
builder.Services.AddWorkflowManagers();

builder.Services.AddHostedService<BotService>();

var host = builder.Build();

await host.RunAsync();

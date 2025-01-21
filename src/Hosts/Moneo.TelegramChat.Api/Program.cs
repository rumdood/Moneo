using Moneo.Chat.ServiceCollectionExtensions;
using Moneo.Chat.Telegram;
using Moneo.Core;
using Moneo.TelegramChat.Api.Configuration;
using Moneo.TelegramChat.Api.Features.GetStatus;
using Moneo.TelegramChat.Api.Features.ReceiveMessage;
using Moneo.TelegramChat.Api.Features.SendBotTextMessage;
using Moneo.TelegramChat.Api.Features.StartChatAdapter;
using Moneo.TelegramChat.Api.Features.StopChatAdapter;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

IBotClientConfiguration botClientConfiguration = new BotClientConfiguration();
builder.Configuration.GetSection("BotClient").Bind(botClientConfiguration);

builder.Services.AddChatAdapter<TelegramChatAdapter>();
builder.Services.AddInMemoryChatStateManagement();
builder.Services.AddChatManager();
builder.Services.AddMemoryCache();
builder.Services.AddScoped(_ => botClientConfiguration);
builder.Services.AddWorkflowManagers();

var app = builder.Build();

app.AddGetStatusEndpoint();
app.AddReceiveMessageEndpoint();
app.AddSendBotTextmessageEndpoint();
app.AddStartChatAdapterEndpoint();
app.AddStopChatAdapterEndpoint();

app.Run();
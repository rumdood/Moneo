using Moneo.Chat.Telegram;
using Moneo.TaskManagement.Api.ServiceCollectionExtensions;
using Moneo.TaskManagement.Api.Services;
using Moneo.TaskManagement.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddTaskManagement(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("TaskManagement"));
});

builder.Services.AddTelegramChatAdapter(opts =>
{
    var masterConversationId = builder.Configuration.GetValue<long>("Telegram:MasterConversationId");
    var botToken =  builder.Configuration["Telegram:BotToken"];
    var callbackToken =  builder.Configuration["Telegram:CallbackToken"];

    if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(callbackToken))
    {
        throw new InvalidOperationException(
            "Telegram:BotToken and Telegram:CallbackToken must be set in the configuration");
    }
    
    opts.MasterConversationId = masterConversationId;
    opts.BotToken = botToken;
    opts.CallbackToken = callbackToken;
    opts.UseInMemoryStateManagement();
});

builder.Services.AddSingleton<ITaskManagerClient, InternalTaskManagerClient>();

var app = builder.Build();
app.MapGet("/about", () => "Moneo Task Management API");
app.UseHealthChecks("/health");

app.AddTaskManagementEndpoints();
app.AddTelegramChatAdapterEndpoints();

app.Run();

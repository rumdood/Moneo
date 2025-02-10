using Moneo.Chat.Telegram;
using Moneo.Hosts.Chat.Api.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// get the TaskManagementConfig from the configuration "Moneo:TaskManagement"
var taskManagementConfig = builder.Configuration.GetSection("Moneo:TaskManagement").Get<TaskManagementConfig>();

if (taskManagementConfig is null)
{
    throw new InvalidOperationException("Moneo:TaskManagement section is missing in the configuration");
}

// register the TaskManagementConfig in the DI container
builder.Services.AddSingleton(taskManagementConfig);

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

builder.Services.AddHealthChecks();

builder.Services.AddTaskManagement(opt =>
{
    opt.UseConfiguration(taskManagementConfig);
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHealthChecks("/health");
app.MapGet("/api/about", () => "Moneo Chat API");
app.MapOpenApi();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "v1");
});
app.AddTelegramChatAdapterEndpoints();

app.Run();
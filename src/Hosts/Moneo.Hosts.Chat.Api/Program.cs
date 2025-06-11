using Moneo.Chat.Telegram;
using Moneo.Hosts.Chat.Api;
using Moneo.Hosts.Chat.Api.Tasks;
using Moneo.TaskManagement.Workflows.CreateTask;
using Moneo.Web.Auth;
using Moneo.Web.Auth.Logging;

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

// get the TaskManagementConfig from the configuration "Moneo:TaskManagement"
var chatConfig = builder.Configuration.GetSection("Moneo:Chat").Get<ChatConfig>();

if (chatConfig is null)
{
    throw new InvalidOperationException("Moneo:Chat section is missing in the configuration");
}

// register the TaskManagementConfig in the DI container
builder.Services.AddSingleton(chatConfig);

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
    
    opts.RegisterUserRequestsAndWorkflowsFromAssemblyContaining<CreateTaskRequest>();
    
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

builder.Services.AddTaskManagementChat();

builder.Services.AddOpenApi();

const string authenticationPolicyName = "Moneo.Chat.ApiKey";

builder.Services.AddApiKeyAuthentication(opt =>
{
    opt.HeaderName = "X-Api-Key";
    opt.UseValidationCallback(apiKey =>
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return Task.FromResult(false);
        }
        
        return Task.FromResult(chatConfig.ApiKey == apiKey);
    });
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(authenticationPolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.ApiKeyAuthenticationScheme);
    });

builder.Services.AddMoneoHttpLogging(builder.Configuration);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", b =>
    {
        b.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseHttpLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/health");
app.MapGet("/api/about", () => "Moneo Chat API");
app.MapOpenApi();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "v1");
});
app.AddTelegramChatAdapterEndpoints(opt =>
{
    opt.RequireAuthorization(authenticationPolicyName);
});

app.Run();
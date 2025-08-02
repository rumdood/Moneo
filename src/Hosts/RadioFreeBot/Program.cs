using Moneo.Chat.Telegram;
using Moneo.Web.Auth;
using RadioFreeBot;
using RadioFreeBot.Configuration;
using RadioFreeBot.Features.AddSongToPlaylist;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSerilog(opt => 
    opt.WriteTo.Console()
        .ReadFrom.Configuration(builder.Configuration));

var radioFreeBotConfiguration = builder.Configuration.GetSection("RadioFree").Get<RadioFreeBotConfiguration>();

if (!builder.Services.IsRadioFreeBotConfigurationValid(radioFreeBotConfiguration))
{
    throw new InvalidOperationException("RadioFreeBot configuration is invalid. Please check the settings.");
}

builder.Services.AddSingleton(radioFreeBotConfiguration!);
builder.Services.AddSingleton(radioFreeBotConfiguration!.YouTubeMusicProxy);

builder.Services.AddPlaylistManagement(opt =>
{
    opt.ConfigureYouTubeProxy(ytopt =>
    {
        ytopt.YouTubeMusicProxyUrl = radioFreeBotConfiguration.YouTubeMusicProxy.YouTubeMusicProxyUrl;
    });
    opt.UseSqliteDatabase(builder.Configuration.GetConnectionString("RadioFree")!);
});

builder.Services.AddTelegramChatAdapter(opts =>
{
    var masterConversationId = builder.Configuration.GetValue<long>("Telegram:MasterConversationId");
    var botToken = builder.Configuration["Telegram:BotToken"];
    var callbackToken = builder.Configuration["Telegram:CallbackToken"];

    if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(callbackToken))
    {
        throw new InvalidOperationException(
            "Telegram:BotToken and Telegram:CallbackToken must be set in the configuration");
    }
    
    opts.RegisterUserRequestsAndWorkflowsFromAssemblyContaining<AddSongRequest>();

    opts.MasterConversationId = masterConversationId;
    opts.BotToken = botToken;
    opts.CallbackToken = callbackToken;
    opts.UseInMemoryStateManagement();
});

var chatApiConfiguration = builder.Configuration.GetSection("Moneo:Chat");

if (chatApiConfiguration is null)
{
    throw new InvalidOperationException("Moneo:Chat configuration is missing");
}

builder.Services.AddSingleton(chatApiConfiguration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

const string authenticationPolicyName = "RadioFreeBot.ApiKey";

builder.Services.AddApiKeyAuthentication(opt =>
{
    opt.HeaderName = "X-Api-Key";
    opt.UseValidationCallback(apiKey =>
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    });
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(authenticationPolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.ApiKeyAuthenticationScheme);
    });

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", b =>
    {
        b.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseHealthChecks("/health");
app.MapGet("/api/about", () => "RadioFree Bot API");
app.MapOpenApi();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "v1");
});
app.AddTelegramChatAdapterEndpoints(opt =>
{
    opt.RequireAuthorization(authenticationPolicyName);
});

// app.UseHttpsRedirection();

app.Run();
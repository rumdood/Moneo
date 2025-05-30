using Moneo.Chat.Telegram;
using Moneo.Chat.Workflows.Chitchat;
using RadioFreeBot;
using RadioFreeBot.Features.AddSongToPlaylist;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var config = builder.Configuration.GetSection("RadioFree:YouTubeProxy").Get<YouTubeProxyOptions>();

if (config is null)
{
    throw new InvalidOperationException("YouTubeProxy configuration is missing");
}

builder.Services.AddSingleton(config);

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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpLogging();
app.UseCors("AllowAll");
app.UseHealthChecks("/health");
app.MapGet("/api/about", () => "Moneo Chat API");
app.MapOpenApi();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "v1");
});
app.AddTelegramChatAdapterEndpoints();

// app.UseHttpsRedirection();

app.Run();
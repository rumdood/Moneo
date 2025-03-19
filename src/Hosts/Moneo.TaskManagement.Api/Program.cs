using Microsoft.AspNetCore.HttpLogging;
using Moneo.TaskManagement.Api.ServiceCollectionExtensions;
using Moneo.TaskManagement.Api.Services;
using Moneo.Web.Auth;
using Moneo.Web.Auth.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var notificationConfig = builder.Configuration.GetSection("Moneo:Notification").Get<NotificationConfig>();

builder.Services.AddTaskManagement(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("TaskManagement"));
});

builder.Services.AddNotificationService(opt =>
{
    opt.UseConfiguration(notificationConfig);
});

const string authenticationPolicyName = "Moneo.Tasks.ApiKey";

var validKey = builder.Configuration.GetValue<string>("Moneo:ApiKey");

// this should probably be switched to load the keys from the database or a secret store
builder.Services.AddApiKeyAuthentication(opt =>
{
    opt.HeaderName = "X-Api-Key";
    opt.UseValidationCallback(apiKey =>
    {
        if (string.IsNullOrEmpty(validKey))
        {
            throw new InvalidOperationException("No API key configured.");
        }

        if (apiKey == validKey)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    });
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

builder.Services.AddMoneoHttpLogging(builder.Configuration);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(authenticationPolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.ApiKeyAuthenticationScheme);
    });

builder.Services.AddOpenApi();

var app = builder.Build();
app.UseCors("AllowAll");
app.UseHttpLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.AddTaskManagementEndpoints(opt =>
{
    opt.RequireAuthorization(authenticationPolicyName);
});

app.MapOpenApi();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "v1");
});

app.Run();

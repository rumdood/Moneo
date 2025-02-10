using Moneo.TaskManagement.Api.ServiceCollectionExtensions;
using Moneo.TaskManagement.Api.Services;

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

builder.Services.AddOpenApi();

var app = builder.Build();
app.UseHealthChecks("/health");

app.AddTaskManagementEndpoints();

app.MapOpenApi();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "v1");
});

app.Run();

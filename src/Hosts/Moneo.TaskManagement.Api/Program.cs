using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moneo.TaskManagement.Api;
using Moneo.TaskManagement.Api.Features.CompleteTask;
using Moneo.TaskManagement.Features.CreateEditTask;
using Moneo.TaskManagement.Features.DeactivateTask;
using Moneo.TaskManagement.Features.DeleteTask;
using Moneo.TaskManagement.Features.GetTaskById;
using Moneo.TaskManagement.Features.GetTasks;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.Scheduling;
using Quartz;
using Quartz.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<MoneoConfiguration>(builder.Configuration.GetSection("Moneo"));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateEditTaskRequest>();
});

builder.Services.AddDbContext<MoneoTasksDbContext>((serviceProvider, options) =>
{
    var moneoConfig = serviceProvider.GetRequiredService<IOptions<MoneoConfiguration>>().Value;

    switch (moneoConfig.DatabaseProvider)
    {
        case DatabaseProvider.Sqlite:
            options.UseSqlite(moneoConfig.ConnectionString);
            break;
        case DatabaseProvider.Postgres:
            options.UseNpgsql(moneoConfig.ConnectionString);
            break;
        default:
            throw new InvalidOperationException("Unknown or missing database provider");
    }
});

builder.Services.AddQuartz();
builder.Services.AddQuartzServer(options =>
{
    options.WaitForJobsToComplete = true;
    options.AwaitApplicationStarted = true;
});

builder.Services.AddSingleton<SchedulerService>();
builder.Services.AddSingleton<ISchedulerService>(provider =>
    provider.GetRequiredService<SchedulerService>());
builder.Services.AddSingleton<IHostedService>(provider =>
    provider.GetRequiredService<SchedulerService>());

var app = builder.Build();

app.MapGet("/about", () => "Moneo Task Management API");

app.AddCreatTaskEndpoint();
app.AddUpdateTaskEndpoints();
app.AddCompleteTaskEndpoint();
app.AddSkipTaskEndpoint();
app.AddDeactivateTaskEndpoints();
app.AddDeleteTaskEndpoints();
app.AddGetTaskByFilterEndpoint();
app.AddGetTasksForConversationEndpoint();
app.AddGetTaskByIdEndpoint();

app.Run();

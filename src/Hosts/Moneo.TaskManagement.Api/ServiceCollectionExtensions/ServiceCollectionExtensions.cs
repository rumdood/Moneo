using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.Features.CreateEditTask;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.Scheduling;
using Quartz;
using Quartz.AspNetCore;

namespace Moneo.TaskManagement.Api.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagement(this IServiceCollection services)
    {
        var defaultOptions = TaskManagementOptions.GetDefault();
        services.AddTaskManagement(defaultOptions);
        return services;
    }
    
    public static IServiceCollection AddTaskManagement(
        this IServiceCollection services, 
        TaskManagementOptions options)
    {
        services.AddSingleton(options.TimeProvider);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreateEditTaskRequest>();
        });
        services.AddDbContext<MoneoTasksDbContext>((_, opt) =>
        {
            switch (options.DatabaseProvider)
            {
                case DatabaseProvider.Sqlite:
                    opt.UseSqlite(options.ConnectionString);
                    break;
                case DatabaseProvider.Postgres:
                    opt.UseNpgsql(options.ConnectionString);
                    break;
                default:
                    throw new InvalidOperationException("Unknown or missing database provider");
            }
        });

        services.AddQuartz();
        services.AddQuartzServer(opt =>
        {
            opt.WaitForJobsToComplete = true;
            opt.AwaitApplicationStarted = true;
        });
        
        services.AddSingleton<SchedulerService>();
        services.AddSingleton<ISchedulerService>(provider =>
            provider.GetRequiredService<SchedulerService>());
        services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<SchedulerService>());

        services.AddHealthChecks()
            .AddDbContextCheck<MoneoTasksDbContext>("MoneoTasksDbContext")
            .AddCheck<SchedulerServiceHealthcheck>("SchedulerService");

        return services;
    }

    public static IServiceCollection AddTaskManagement(
        this IServiceCollection services,
        Action<TaskManagementOptions> options)
    {
        var taskManagementOptions = new TaskManagementOptions();
        options.Invoke(taskManagementOptions);
        return services.AddTaskManagement(taskManagementOptions);
    }
}

public class TaskManagementOptions
{
    private const string DefaultConnectionString = "Data Source=taskmanagement.db";
    public string ConnectionString { get; private set; } = DefaultConnectionString;
    public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.Sqlite;
    public TimeProvider TimeProvider { get; private set; } = TimeProvider.System;

    public void ConfigureTimeProvider(Action<TimeProvider> configure)
    {
        configure(TimeProvider);
    }
    
    public void UsePostgres(string connectionString)
    {
        DatabaseProvider = DatabaseProvider.Postgres;
        ConnectionString = connectionString;
    }
    
    public void UseSqlite(string? connectionString)
    {
        DatabaseProvider = DatabaseProvider.Sqlite;
        ConnectionString = connectionString ?? DefaultConnectionString;
    }

    public static TaskManagementOptions GetDefault()
    {
        return new TaskManagementOptions();
    }
}
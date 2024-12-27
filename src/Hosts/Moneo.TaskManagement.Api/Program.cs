using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moneo.TaskManagement.Api;
using Moneo.TaskManagement.ResourceAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ContentRootPath = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.Configure<MoneoConfiguration>(builder.Configuration.GetSection("Moneo"));
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

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
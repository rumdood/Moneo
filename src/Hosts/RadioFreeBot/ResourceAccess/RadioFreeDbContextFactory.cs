using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RadioFreeBot.ResourceAccess;

/// <summary>
/// This factory allows Entity Framework migrations and other design-time tools to create
/// an instance of RadioFreeDbContext without dependency injection.
/// </summary>
public class RadioFreeDbContextFactory : IDesignTimeDbContextFactory<RadioFreeDbContext>
{
    public RadioFreeDbContext CreateDbContext(string[] args)
    {
        // Build a configuration that can be used at design time
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get the connection string from configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=radioFree.sqlite";

        var optionsBuilder = new DbContextOptionsBuilder<RadioFreeDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new RadioFreeDbContext(optionsBuilder.Options);
    }
}

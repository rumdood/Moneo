using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Moneo.Notify;
using Moneo.Notify.Engines;
using Moneo.Core;
using Serilog;
using Serilog.Events;
using Microsoft.ApplicationInsights.Extensibility;

[assembly: FunctionsStartup(typeof(Moneo.Functions.Startup))]
namespace Moneo.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Moneo.Functions", LogEventLevel.Verbose)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Worker", LogEventLevel.Warning)
                .MinimumLevel.Override("Host", LogEventLevel.Warning)
                .MinimumLevel.Override("Function", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure", LogEventLevel.Warning)
                .MinimumLevel.Override("DurableTask", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.ApplicationInsights(
                    TelemetryConfiguration.CreateDefault(),
                    TelemetryConverter.Events,
                    LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            //builder.Services.AddLogging();
            builder.Services.AddScoped<INotifyEngine, TelegramNotify>();
            builder.Services.AddSingleton<IScheduleManager, ScheduleManager>();
            builder.Services.AddSingleton<IMoneoTaskFactory, MoneoTaskFactory>();

            builder.Services.AddLogging(cfg =>
            {
                cfg.AddSerilog(Log.Logger, true);
            });

            /*
            // Is this how you're supposed to make serilog work in AzFn when actually deployed?
            builder.Services.AddSingleton(Log.Logger)
                .AddSingleton<ILoggerProvider>(new Serilog.Extensions.Logging.SerilogLoggerProvider(Log.Logger, dispose: true));
            */
        }
    }
}

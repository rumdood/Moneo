using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Moneo.Notify;
using Moneo.Notify.Engines;
using Moneo.Core;

[assembly: FunctionsStartup(typeof(Moneo.Functions.Startup))]
namespace Moneo.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddScoped<INotifyEngine, TelegramNotify>();
            builder.Services.AddSingleton<IScheduleManager, ScheduleManager>();
            builder.Services.AddSingleton<IMoneoTaskFactory, MoneoTaskFactory>();
        }
    }
}

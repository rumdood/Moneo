using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Moneo.Functions.NotifyEngines
{
    public class DummyNotify : INotifyEngine
    {
        private readonly ILogger<DummyNotify> _logger;

        public DummyNotify(ILogger<DummyNotify> logger)
        {
            _logger = logger;
        }

        public Task SendNotification(long chatId, string message)
        {
            _logger.LogInformation("SENDING NOTIFICATION");
            return Task.CompletedTask;
        }
    }
}

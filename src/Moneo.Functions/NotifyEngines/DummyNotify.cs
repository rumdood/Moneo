using Microsoft.Extensions.Logging;
using Moneo.Notify;
using System.Threading.Tasks;

namespace Moneo.Functions.NotifyEngines
{
    public class DummyNotify : INotifyEngine
    {
        private ILogger<DummyNotify> _logger;

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

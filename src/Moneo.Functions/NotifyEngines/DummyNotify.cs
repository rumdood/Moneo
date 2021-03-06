using Microsoft.Extensions.Logging;
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

        public Task SendDefuseMessage()
        {
            return SendNotification("defuser");
        }

        public Task SendNotification(string message)
        {
            _logger.LogInformation("SENDING NOTIFICATION");
            return Task.CompletedTask;
        }

        public Task SendReminder()
        {
            return SendNotification("reminder");
        }
    }
}

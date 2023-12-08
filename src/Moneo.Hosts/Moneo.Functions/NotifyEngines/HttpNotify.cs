using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moneo.Functions.Chat;

namespace Moneo.Functions.NotifyEngines;

internal class HttpNotify : INotifyEngine
{
    private readonly IChatServiceProxy _proxy;
    private readonly ILogger<HttpNotify> _logger;

    public HttpNotify(IChatServiceProxy proxy, ILogger<HttpNotify> logger)
    {
        _logger = logger;
        _proxy = proxy;
    }
    
    public async Task SendNotification(long chatId, string message)
    {
        _logger.LogDebug("SendNotification: {0}", new { chatId, message });
        await _proxy.SendTextMessageToUserAsync(chatId, message);
    }
}
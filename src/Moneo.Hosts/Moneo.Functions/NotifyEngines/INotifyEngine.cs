using System.Threading.Tasks;

namespace Moneo.Functions.NotifyEngines
{
    public interface INotifyEngine
    {
        Task SendNotification(long chatId, string message);
    }
}

using System.Threading.Tasks;

namespace Moneo.Notify
{
    public interface INotifyEngine
    {
        Task SendNotification(long chatId, string message);
    }
}

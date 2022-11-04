using System.Threading.Tasks;

namespace Moneo.Notify
{
    public interface INotifyEngine
    {
        Task SendNotification(string message);
    }
}

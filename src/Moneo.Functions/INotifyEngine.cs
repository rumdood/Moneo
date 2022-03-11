using System.Threading.Tasks;

namespace Moneo.Functions
{
    public interface INotifyEngine
    {
        Task SendNotification(string message);
    }
}

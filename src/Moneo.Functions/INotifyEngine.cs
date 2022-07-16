using System.Threading.Tasks;

namespace Moneo.Functions
{
    public interface INotifyEngine
    {
        Task SendReminder();
        Task SendDefuseMessage();
    }
}

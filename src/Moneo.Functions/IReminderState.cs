using System;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    public interface IReminderState
    {
        Task Defuse();
        Task<DateTime> GetLastDefusedTimestamp();
        Task Delete();
        Task CheckSendReminder();
    }
}

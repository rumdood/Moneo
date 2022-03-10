using System;
using System.Threading.Tasks;

namespace Moneo.Functions
{
    public interface IReminderState
    {
        void Defuse();
        Task<DateTime> GetLastDefusedTimestamp();
        void Delete();
    }
}

namespace Moneo.Functions.Isolated.TaskManager
{
    public interface INotifyEngine
    {
        Task SendNotification(long chatId, string message);
    }
}

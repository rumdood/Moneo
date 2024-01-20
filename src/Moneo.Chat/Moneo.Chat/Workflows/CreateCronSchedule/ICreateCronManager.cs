using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

public interface ICreateCronManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
}
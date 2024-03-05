using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

public interface ICreateCronWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
}
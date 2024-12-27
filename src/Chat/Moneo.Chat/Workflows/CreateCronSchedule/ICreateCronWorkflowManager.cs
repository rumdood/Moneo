using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

public interface ICreateCronWorkflowManager : IWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId, ChatState? outerChatState = null);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
}
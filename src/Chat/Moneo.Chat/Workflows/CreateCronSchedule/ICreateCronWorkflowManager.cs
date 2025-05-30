using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateCronSchedule;

public interface ICreateCronWorkflowManager : IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId, long forUserId, ChatState? outerChatState = null, CancellationToken cancellationToken = default);
}
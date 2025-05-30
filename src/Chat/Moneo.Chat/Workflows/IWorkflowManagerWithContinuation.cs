using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows;

public interface IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> ContinueWorkflowAsync(
        long chatId, 
        long forUserId,
        string userInput, 
        CancellationToken cancellationToken = default);
}
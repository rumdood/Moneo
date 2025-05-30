using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows;

public interface IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> ContinueWorkflowAsync(
        CommandContext context,
        string userInput, 
        CancellationToken cancellationToken = default);
}
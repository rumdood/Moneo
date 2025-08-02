using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.Chitchat;

public interface IChitChatWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string userInput, CancellationToken cancellationToken = default);
}
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.Chitchat;

public interface IChitChatWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(long chatId, long forUserId, string userInput, CancellationToken cancellationToken = default);
}
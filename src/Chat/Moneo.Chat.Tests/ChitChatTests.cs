using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.Chat.Workflows.Chitchat;

namespace Moneo.Chat.Tests;

public class ChitChatTests
{
    // create a class that implements the IChitChatWorkflowManager interface
    [MoneoWorkflow]
    public class ChitChatWorkflowManager : WorkflowManagerBase, IChitChatWorkflowManager
    {
        private readonly ICommandStateRegistry _commandStateRegistry;

        public ChitChatWorkflowManager(IMediator mediator, ICommandStateRegistry commandStateRegistry) : base(mediator)
        {
            _commandStateRegistry = commandStateRegistry;
        }

        public Task<MoneoCommandResult> ContinueWorkflowAsync(
            CommandContext context, 
            string userInput, 
            CancellationToken cancellationToken = default)
        {
            // Implement the logic to continue the workflow based on user input
            return Task.FromResult(new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = $"Continuing workflow with input: {userInput}"
            });
        }

        public Task<MoneoCommandResult> StartWorkflowAsync(CommandContext cmdContext, string userInput, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = $"Received input: {userInput}. This is a placeholder response for the ChitChat workflow."
            });
        }
    }
    
    private readonly IChitChatWorkflowManager _chitChatWorkflowManager;
    
    
}
using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat;

public interface IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> ContinueWorkflowAsync(
        long chatId, 
        string userInput, 
        CancellationToken cancellationToken = default);
}

public abstract class WorkflowManagerBase
{
    protected readonly IMediator Mediator;

    protected WorkflowManagerBase(IMediator mediator)
    {
        Mediator = mediator;
    }
}
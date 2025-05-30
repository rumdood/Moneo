using MediatR;

namespace Moneo.Chat.Workflows;

public abstract class WorkflowManagerBase
{
    protected readonly IMediator Mediator;

    protected WorkflowManagerBase(IMediator mediator)
    {
        Mediator = mediator;
    }
}

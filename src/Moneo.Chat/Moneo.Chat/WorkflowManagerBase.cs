using MediatR;

namespace Moneo.Chat
{
    public abstract class WorkflowManagerBase
    {
        protected readonly IMediator Mediator;

        protected WorkflowManagerBase(IMediator mediator)
        {
            Mediator = mediator;
        }
    }
}

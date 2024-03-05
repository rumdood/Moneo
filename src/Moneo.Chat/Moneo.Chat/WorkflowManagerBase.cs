using MediatR;

namespace Moneo.Chat
{
    public abstract class WorkflowManagerBase
    {
        protected readonly IMediator _mediator;

        protected WorkflowManagerBase(IMediator mediator)
        {
            _mediator = mediator;
        }
    }
}

using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.ChangeTask
{
    internal class ChangeTaskRequestHandler : IRequestHandler<ChangeTaskRequest, MoneoCommandResult>
    {
        private readonly IChangeTaskWorkflowManager _manager;

        public ChangeTaskRequestHandler(IChangeTaskWorkflowManager manager)
        {
            _manager = manager;
        }

        public async Task<MoneoCommandResult> Handle(ChangeTaskRequest request, CancellationToken cancellationToken)
        {
            return await _manager.StartWorkflowAsync(request.Context, request.TaskName, cancellationToken);
        }
    }
}

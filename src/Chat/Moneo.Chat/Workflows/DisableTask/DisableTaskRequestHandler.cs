using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.DisableTask;

internal class DisableTaskRequestHandler : IRequestHandler<DisableTaskRequest, MoneoCommandResult>
{
    private readonly IDisableTaskWorkflowManager _workflowManager;

    public DisableTaskRequestHandler(IDisableTaskWorkflowManager taskManager)
    {
        _workflowManager = taskManager;
    }

    public Task<MoneoCommandResult> Handle(DisableTaskRequest request, CancellationToken cancellationToken) =>
        _workflowManager.StartWorkflowAsync(request.Context, request.TaskName, cancellationToken);
}

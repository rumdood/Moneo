using MediatR;
using Moneo.Chat.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneo.Chat.Workflows.DisableTask;

internal class DisableTaskRequestHandler : IRequestHandler<DisableTaskRequest, MoneoCommandResult>
{
    private readonly IDisableTaskWorkflowManager _workflowManager;

    public DisableTaskRequestHandler(IDisableTaskWorkflowManager taskManager)
    {
        _workflowManager = taskManager;
    }

    public Task<MoneoCommandResult> Handle(DisableTaskRequest request, CancellationToken cancellationToken) =>
        _workflowManager.StartWorkflowAsync(request.ConversationId, request.ForUserId, request.TaskName, cancellationToken);
}

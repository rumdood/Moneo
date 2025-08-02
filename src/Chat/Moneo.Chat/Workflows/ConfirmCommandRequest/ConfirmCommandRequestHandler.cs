using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.Commands;

namespace Moneo.Chat;

internal class ConfirmCommandRequestHandler : IRequestHandler<ConfirmCommandRequest, MoneoCommandResult>
{
    private readonly IConfirmCommandWorkflowManager _manager;

    public ConfirmCommandRequestHandler(IConfirmCommandWorkflowManager manager)
    {
        _manager = manager;
    }

    public async Task<MoneoCommandResult> Handle(ConfirmCommandRequest request, CancellationToken cancellationToken = default)
    {
        return await _manager.StartWorkflowAsync(request, cancellationToken);
    }
}

using MediatR;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using Moneo.TaskManagement.Workflows;

namespace Moneo.TaskManagement.Chat.UserRequests;

internal class SkipTaskRequestHandler : IRequestHandler<SkipTaskRequest, MoneoCommandResult>
{
    private readonly ICompleteTaskWorkflowManager _manager;

    public SkipTaskRequestHandler(ICompleteTaskWorkflowManager manager)
    {
        _manager = manager;
    }
    
    public async Task<MoneoCommandResult> Handle(SkipTaskRequest request, CancellationToken cancellationToken) =>
        await _manager.StartWorkflowAsync(
            request.Context,
            request.TaskName, 
            CompleteTaskOption.Skip,
            cancellationToken);
}

using MediatR;
using Moneo.Chat.Commands;
using Moneo.TaskManagement.Workflows;

namespace Moneo.TaskManagement.Chat.UserRequests;

internal class CompleteTaskRequestHandler : IRequestHandler<CompleteTaskRequest, MoneoCommandResult>
{
    private readonly ICompleteTaskWorkflowManager _manager;

    public CompleteTaskRequestHandler(ICompleteTaskWorkflowManager manager)
    {
        _manager = manager;
    }
    
    public Task<MoneoCommandResult> Handle(CompleteTaskRequest request, CancellationToken cancellationToken) =>
        _manager.StartWorkflowAsync(
            request.Context,
            request.TaskName, 
            CompleteTaskOption.Complete,
            cancellationToken);
}

using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat.Workflows.CreateTask;

internal class CreateTaskRequestHandler : IRequestHandler<CreateTaskRequest, MoneoCommandResult>
{
    private readonly ICreateTaskWorkflowManager _manager;

    public CreateTaskRequestHandler(ICreateTaskWorkflowManager manager)
    {
        _manager = manager;
    }
    
    public async Task<MoneoCommandResult> Handle(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        return await _manager.StartWorkflowAsync(request.Context, request.TaskName);
    }
}
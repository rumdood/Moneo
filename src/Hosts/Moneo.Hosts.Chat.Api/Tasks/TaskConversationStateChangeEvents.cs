using MediatR;
using Moneo.Chat;

namespace Moneo.Hosts.Chat.Api.Tasks;

public record CreateTaskWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record CreateTaskWorkflowCompletedEvent(long ChatId, long UserId) : IRequest;

public record ChangeTaskWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record ChangeTaskWorkflowCompletedEvent(long ChatId, long UserId) : IRequest;

internal class CreateTaskWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateTaskWorkflowStartedEvent>
{
    public async Task Handle(CreateTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, request.UserId, TaskChatStates.CreateTask);
    }
}

internal class CreateTaskWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateTaskWorkflowCompletedEvent>
{
    public async Task Handle(CreateTaskWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId, request.UserId);
    }
}

internal class ChangeTaskWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ChangeTaskWorkflowStartedEvent>
{
    public async Task Handle(ChangeTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, request.UserId, TaskChatStates.ChangeTask);
    }
}

internal class ChangeTaskWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ChangeTaskWorkflowCompletedEvent>
{
    public async Task Handle(ChangeTaskWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId, request.UserId);
    }
}



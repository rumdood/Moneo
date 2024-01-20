using MediatR;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat;

public record CreateTaskWorkflowStartedEvent(long ChatId) : IRequest;

public record CreateTaskWorkflowCompletedEvent(long ChatId) : IRequest;

public record CreateCronWorkflowStartedEvent(long ChatId) : IRequest;

public record CreateCronWorkflowCompletedEvent(long ChatId, string CronStatement) : IRequest;

internal abstract class WorkflowStartedOrCompletedEventHandlerBase
{
    protected readonly IChatStateRepository ChatStateRepository;

    public WorkflowStartedOrCompletedEventHandlerBase(IChatStateRepository chatStateRepository)
    {
        ChatStateRepository = chatStateRepository;
    }
}

internal class CreateTaskWorklowStartedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<CreateTaskWorkflowStartedEvent>
{
    public CreateTaskWorklowStartedEventHandler(IChatStateRepository chatStateRepository) : base(chatStateRepository)
    {
    }

    public async Task Handle(CreateTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, ChatState.CreateTask);
    }
}

internal class CreateTaskWorkflowCompletedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<CreateTaskWorkflowCompletedEvent>
{
    public CreateTaskWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository) : base(chatStateRepository)
    {
    }

    public async Task Handle(CreateTaskWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}

internal class CreateCronWorkflowStartedOrStartedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<CreateCronWorkflowStartedEvent>
{
    public CreateCronWorkflowStartedOrStartedEventHandler(IChatStateRepository chatStateRepository) : base(
        chatStateRepository)
    {
    }

    public async Task Handle(CreateCronWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, ChatState.CreateCron);
    }
}

internal class CreateCronWorkflowStartedOrCompletedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<CreateCronWorkflowCompletedEvent>
{
    public CreateCronWorkflowStartedOrCompletedEventHandler(IChatStateRepository chatStateRepository, IMediator mediator) : base(
        chatStateRepository)
    {
    }

    public async Task Handle(CreateCronWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}
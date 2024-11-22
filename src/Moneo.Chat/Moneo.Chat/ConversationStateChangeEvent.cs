using MediatR;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat;

public record CreateTaskWorkflowStartedEvent(long ChatId) : IRequest;

public record CreateTaskWorkflowCompletedEvent(long ChatId) : IRequest;

public record ChangeTaskWorkflowStartedEvent(long ChatId) : IRequest;

public record ChangeTaskWorkflowCompletedEvent(long ChatId) : IRequest;

public record ConfirmCommandWorkflowStartedEvent(long ChatId) : IRequest;

public record ConfirmCommandWorkflowCompletedEvent(long ChatId, string? userInput = null) : IRequest;

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

internal class ChangeTaskWorkflowStartedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<ChangeTaskWorkflowStartedEvent>
{
    public ChangeTaskWorkflowStartedEventHandler(IChatStateRepository chatStateRepository) : base(chatStateRepository)
    {
    }

    public async Task Handle(ChangeTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}

internal class ConfirmCommandWorkflowStartedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<ConfirmCommandWorkflowStartedEvent>
{
    public ConfirmCommandWorkflowStartedEventHandler(IChatStateRepository chatStateRepository) : base(chatStateRepository)
    {
    }

    public async Task Handle(ConfirmCommandWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, ChatState.ConfirmCommand);
    }
}

internal class ConfirmCommandWorkflowCompletedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<ConfirmCommandWorkflowCompletedEvent>
{
    public ConfirmCommandWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository) : base(chatStateRepository)
    {
    }

    public async Task Handle(ConfirmCommandWorkflowCompletedEvent request, CancellationToken cancellationToken)
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
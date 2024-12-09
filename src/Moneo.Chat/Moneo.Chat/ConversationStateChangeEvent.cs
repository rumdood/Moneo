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

internal abstract class WorkflowStartedOrCompletedEventHandlerBase(IChatStateRepository chatStateRepository)
{
    protected readonly IChatStateRepository ChatStateRepository = chatStateRepository;
}

internal class CreateTaskWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateTaskWorkflowStartedEvent>
{
    public async Task Handle(CreateTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, ChatState.CreateTask);
    }
}

internal class CreateTaskWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateTaskWorkflowCompletedEvent>
{
    public async Task Handle(CreateTaskWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}

internal class ChangeTaskWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ChangeTaskWorkflowStartedEvent>
{
    public async Task Handle(ChangeTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, ChatState.ChangeTask);
    }
}

internal class ChangeTaskWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ChangeTaskWorkflowCompletedEvent>
{
    public async Task Handle(ChangeTaskWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}

internal class ConfirmCommandWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ConfirmCommandWorkflowStartedEvent>
{
    public async Task Handle(ConfirmCommandWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, ChatState.ConfirmCommand);
    }
}

internal class ConfirmCommandWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ConfirmCommandWorkflowCompletedEvent>
{
    public async Task Handle(ConfirmCommandWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId);
    }
}

internal class CreateCronWorkflowStartedOrStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateCronWorkflowStartedEvent>
{
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
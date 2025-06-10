using MediatR;
using Moneo.Chat.Workflows.CreateTask;

namespace Moneo.Chat;

public record CreateTaskWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record CreateTaskWorkflowCompletedEvent(long ChatId, long UserId) : IRequest;

public record ChangeTaskWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record ChangeTaskWorkflowCompletedEvent(long ChatId, long UserId) : IRequest;

public record ConfirmCommandWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record ConfirmCommandWorkflowCompletedEvent(long ChatId, long UserId, string? userInput = null) : IRequest;

public record CreateCronWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record CreateCronWorkflowCompletedEvent(long ChatId, long UserId, string CronStatement) : IRequest;

public abstract class WorkflowStartedOrCompletedEventHandlerBase(IChatStateRepository chatStateRepository)
{
    protected readonly IChatStateRepository ChatStateRepository = chatStateRepository;
}

internal class CreateTaskWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateTaskWorkflowStartedEvent>
{
    public async Task Handle(CreateTaskWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, request.UserId, ChatState.CreateTask);
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
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, request.UserId, ChatState.ChangeTask);
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

internal class ConfirmCommandWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ConfirmCommandWorkflowStartedEvent>
{
    public async Task Handle(ConfirmCommandWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, request.UserId, ChatState.ConfirmCommand);
    }
}

internal class ConfirmCommandWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<ConfirmCommandWorkflowCompletedEvent>
{
    public async Task Handle(ConfirmCommandWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId, request.UserId);
    }
}

internal class CreateCronWorkflowStartedEventHandler(IChatStateRepository chatStateRepository)
    : WorkflowStartedOrCompletedEventHandlerBase(chatStateRepository),
        IRequestHandler<CreateCronWorkflowStartedEvent>
{
    public async Task Handle(CreateCronWorkflowStartedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.UpdateChatStateAsync(request.ChatId, request.UserId, ChatState.CreateCron);
    }
}

internal class CreateCronWorkflowCompletedEventHandler : WorkflowStartedOrCompletedEventHandlerBase,
    IRequestHandler<CreateCronWorkflowCompletedEvent>
{
    public CreateCronWorkflowCompletedEventHandler(IChatStateRepository chatStateRepository, IMediator mediator) : base(
        chatStateRepository)
    {
    }

    public async Task Handle(CreateCronWorkflowCompletedEvent request, CancellationToken cancellationToken)
    {
        await ChatStateRepository.RevertChatStateAsync(request.ChatId, request.UserId);
    }
}
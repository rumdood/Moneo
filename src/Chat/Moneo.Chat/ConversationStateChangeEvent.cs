using MediatR;

namespace Moneo.Chat;

public record ConfirmCommandWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record ConfirmCommandWorkflowCompletedEvent(long ChatId, long UserId, string? userInput = null) : IRequest;

public record CreateCronWorkflowStartedEvent(long ChatId, long UserId) : IRequest;

public record CreateCronWorkflowCompletedEvent(long ChatId, long UserId, string CronStatement) : IRequest;

public abstract class WorkflowStartedOrCompletedEventHandlerBase(IChatStateRepository chatStateRepository)
{
    protected readonly IChatStateRepository ChatStateRepository = chatStateRepository;
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
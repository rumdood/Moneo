using MediatR;
using Moneo.Chat.BotResponses;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;

namespace Moneo.Chat;

public interface IConfirmCommandWorkflowManager : IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> StartWorkflowAsync(ConfirmCommandRequest request, CancellationToken cancellationToken);
}

[MoneoWorkflow]
public class ConfirmCommandWorkflowManager : WorkflowManagerBase, IConfirmCommandWorkflowManager
{
    private readonly Dictionary<long, string> _userCommandsLookup = new();

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(CommandContext cmdContext, string userInput, CancellationToken cancellationToken = default)
    {
        var confirmation = UserConfirmationHelper.GetConfirmation(userInput);

        if (confirmation == UserConfirmation.Unknown)
        {
            return new MoneoCommandResult()
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = BotResponseHelper.GetBotResponse(BotResponseType.RequestYesOrNo)
            };
        }
        
        // the workflow completes regardless of what the user says at this point. Either we confirm the command and
        // run it, or we're unclear on what they meant to do
        await Mediator.Send(new ConfirmCommandWorkflowCompletedEvent(cmdContext.ConversationId, cmdContext.User?.Id ?? 0), cancellationToken);

        if (confirmation == UserConfirmation.Negative)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = BotResponseHelper.GetBotResponse(BotResponseType.UnsureHowToProceed)
            };
        }

        var foundCommand = _userCommandsLookup.TryGetValue(cmdContext.ConversationId, out var command);
        _userCommandsLookup.Remove(cmdContext.ConversationId);

        if (!foundCommand || command is null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "I'm sorry, something went wrong. What was it you wanted to do?"
            };
        }

        // I don't like this part and user request management like this should probably be moved into another class to be called by the managers
        var context = CommandContextFactory.BuildCommandContext(cmdContext.ConversationId, cmdContext.User, ChatState.ConfirmCommand, command);
        var userRequest = UserRequestFactory.GetUserRequest(context);

        if (userRequest is IRequest<MoneoCommandResult> request)
        {
            return await Mediator.Send(request, cancellationToken);
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.Error,
            UserMessageText = $"Unknown command: {context.CommandKey}"
        };

    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(ConfirmCommandRequest request, CancellationToken cancellationToken = default)
    {
        _userCommandsLookup[request.Context.ConversationId] = $"{request.PotentialCommand} {request.PotentialArguments}";

        await Mediator.Send(new ConfirmCommandWorkflowStartedEvent(request.Context.ConversationId, request.Context.User?.Id ?? 0), cancellationToken);
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = $"Do you want to {request.PotentialCommand} {request.PotentialArguments}?"
        };
    }

    public ConfirmCommandWorkflowManager(IMediator mediator) : base(mediator)
    {
    }
}

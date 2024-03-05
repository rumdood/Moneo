﻿using MediatR;
using Moneo.Chat.Commands;

namespace Moneo.Chat;

public interface IConfirmCommandWorkflowManager
{
    Task<MoneoCommandResult> StartWorkflowAsync(ConfirmCommandRequest request);
    Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput);
}

public class ConfirmCommandWorkflowManager : WorkflowManagerBase, IConfirmCommandWorkflowManager
{
    private readonly Dictionary<long, string> _userCommandsLookup = new();

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(long chatId, string userInput)
    {
        // the workflow completes regardless of what the user says at this point. Either we confirm the command and run it, or we're doign chit-chat
        await _mediator.Send(new ConfirmCommandWorkflowCompletedEvent(chatId));

        if (userInput.Equals("yes", StringComparison.OrdinalIgnoreCase) || userInput.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            var foundCommand = _userCommandsLookup.TryGetValue(chatId, out var command);
            _userCommandsLookup.Remove(chatId);

            if (!foundCommand || command is null)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = "I'm sorry, I don't know what you're trying to confirm"
                };
            }

            // I don't like this part and user request management like this should probably be moved into another class to be called by the managers
            var context = CommandContext.Get(chatId, ChatState.ConfirmCommand, command);
            var userRequest = UserRequestFactory.GetUserRequest(context);

            if (userRequest is IRequest<MoneoCommandResult> request)
            {
                return await _mediator.Send(request);
            }

            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"Unknown command: {context.CommandKey}"
            };
        }
        else
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "I'm sorry - I'm not sure what to do then"
            };
        }
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(ConfirmCommandRequest request)
    {
        _userCommandsLookup[request.ConversationId] = $"{request.PotentialCommand} {request.PotentialArguments}";

        await _mediator.Send(new ConfirmCommandWorkflowStartedEvent(request.ConversationId));
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
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using Moneo.Common;

namespace Moneo.Chat;

internal class ChatManager : IChatManager
{
    private readonly Dictionary<long, FixedLengthList<ChatEntry>> _conversationsById = new ();
    private readonly Dictionary<long, HashSet<long>> _usersByConversationId = new();
    private readonly Dictionary<long, ChatUser> _usersById = new();
    
    private readonly IMediator _mediator;
    private readonly ILogger<ChatManager> _logger;
    private readonly IChatStateRepository _chatStateRepository;

    public ChatManager(IMediator mediator, ILogger<ChatManager> logger, IChatStateRepository stateRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _chatStateRepository = stateRepository;
    }

    public IEnumerable<ChatEntry> GetLastEntriesForConversation(long conversationId, int count)
    {
        return _conversationsById.TryGetValue(conversationId, out var messages)
            ? messages.Take(count)
            : [];
    }

    public async Task ProcessUserMessageAsync(UserMessage message)
    {
        // dump the usermessage to the log as JSON
        _logger.LogDebug("Received From User: {UserMessageJson}", JsonSerializer.Serialize(message));
        
        if (!_usersByConversationId.ContainsKey(message.ConversationId))
        {
            if (message.FromUser is not null)
            {
                if (!_usersByConversationId.TryGetValue(message.ConversationId, out var users))
                {
                    users = [];
                }

                users.Add(message.FromUser.Id);
                _usersByConversationId[message.ConversationId] = users;
                
                if (!_usersById.ContainsKey(message.FromUser.Id))
                {
                    _usersById[message.FromUser.Id] = message.FromUser;
                }
            }
        }

        var result = await ProcessCommandAsync(message.ConversationId, message.FromUser, message.Text);

        _logger.LogDebug("Handling {@Command}", message.Text);
        
        _logger.LogDebug("{@Result}", result);
        switch (result.ResponseType)
        {
            case ResponseType.None:
                break;
            case ResponseType.Text:
                await _mediator.Send(new BotTextMessageRequest(message.ConversationId, result.UserMessageText ?? "",
                    result.Type == ResultType.Error));
                break;
            case ResponseType.Animation:
                await _mediator.Send(new BotGifMessageRequest(message.ConversationId, result.UserMessageText!));
                break;
            case ResponseType.Media:
            case ResponseType.Menu:
                await _mediator.Send(new BotMenuMessageRequest(message.ConversationId, result.UserMessageText ?? "Select one of the following:",
                    result.MenuOptions));
                break;
            default:
                throw new InvalidOperationException($"Unknown bot response type: {result.ResponseType}");
        }
    }

    public Task<ChatState> GetChatStateForConversationAsync(long conversationId)
    {
        return _chatStateRepository.GetChatStateAsync(conversationId);
    }

    private async Task<MoneoCommandResult> ProcessCommandAsync(long conversationId, ChatUser? user, string? text)
    {
        var state = await _chatStateRepository.GetChatStateAsync(conversationId);
        
        _logger.LogDebug("Current Chat State for conversation {@Conversation} is {@State}", conversationId, state);

        var context = CommandContextFactory.BuildCommandContext(conversationId, user?.Id ?? 0, state, text!);

        try
        {
            var userRequest = UserRequestFactory.GetUserRequest(context);
            
            if (userRequest is IRequest<MoneoCommandResult> request)
            {
                return await _mediator.Send(request);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to create user request for command: {Command}", text);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = ex.Message
            };
        }

        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.Error,
            UserMessageText = $"Unknown command: {context.CommandKey}"
        };
    }
}
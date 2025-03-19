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
    private readonly Dictionary<long, User> _usersByConversationId = new();
    
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
        _logger.LogDebug("Received From User: {@Message}", message.Text);
        
        if (!_usersByConversationId.ContainsKey(message.ConversationId))
        {
            if (string.IsNullOrEmpty(message.UserFirstName))
            {
                throw new InvalidOperationException("User has no username");
            }
            
            _ = AddUser(message.ConversationId, message.UserFirstName, message.UserLastName);
        }

        var result = await ProcessCommandAsync(message.ConversationId, message.Text);

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


    public bool AddUser(long conversationId, string firstName, string? lastName)
    {
        return _usersByConversationId.TryAdd(conversationId, new User(Guid.NewGuid(), firstName, lastName, conversationId));
    }

    private async Task<MoneoCommandResult> ProcessCommandAsync(long conversationId, string text)
    {
        var state = await _chatStateRepository.GetChatStateAsync(conversationId);
        
        _logger.LogDebug("Current Chat State for conversation {@Conversation} is {@State}", conversationId, state);

        var context = CommandContext.Get(conversationId, state, text);
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
}
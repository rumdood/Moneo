using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Bot.BotRequests;
using Moneo.Bot.Commands;
using Moneo.Bot.UserRequests;
using Moneo.Bot.Workflows.Chitchat;
using Moneo.Core;
using Telegram.Bot.Types;

namespace Moneo.Bot;

internal enum MessageDirection
{
    UserToBot,
    BotToUser
}

internal record User(Guid Id, string FirstName, string? LastName, long ConversationId);

internal record ConversationEntry(long ConversationId, User ForUser, string Message, MessageDirection Direction, DateTimeOffset TimeStamp);

internal interface IConversationManager
{
    IEnumerable<ConversationEntry> GetLastEntriesForConversation(long conversationId, int count);
    Task ProcessUserMessageAsync(UserMessage message);
    bool AddUser(long conversationId, string firstName, string? lastName);
}

internal class ConversationManager : IConversationManager
{
    private readonly Dictionary<long, FixedLengthList<ConversationEntry>> _conversationsById = new ();
    private readonly Dictionary<long, User> _usersByConversationId = new();
    private readonly IMediator _mediator;
    private readonly ILogger<ConversationManager> _logger;

    public ConversationManager(IMediator mediator, ILogger<ConversationManager> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public IEnumerable<ConversationEntry> GetLastEntriesForConversation(long conversationId, int count)
    {
        return _conversationsById.TryGetValue(conversationId, out var messages)
            ? messages.Take(count)
            : Enumerable.Empty<ConversationEntry>();
    }

    public async Task ProcessUserMessageAsync(UserMessage message)
    {
        _logger.LogInformation("{@Message}", message);
        
        if (!_usersByConversationId.ContainsKey(message.ConversationId))
        {
            if (string.IsNullOrEmpty(message.UserFirstName))
            {
                throw new InvalidOperationException("User has no username");
            }
            
            _ = AddUser(message.ConversationId, message.UserFirstName, message.UserLastName);
        }

        var messageParts = message.Text.Split(' ');

        if (messageParts is null || messageParts.Length == 0)
        {
            throw new InvalidOperationException("Empty message received from user");
        }

        var cmd = messageParts[0];
        var taskName = message.Text[message.Text.IndexOf(" ", StringComparison.Ordinal)..];
        
        _logger.LogDebug("Handling {@Command}", message.Text);

        var resultTask = cmd switch
        {
            CompleteTaskRequest.CommandKey => _mediator.Send(new CompleteTaskRequest(message.ConversationId, taskName)),
            SkipTaskRequest.CommandKey => _mediator.Send(new SkipTaskRequest(message.ConversationId, taskName)),
            _ => _mediator.Send(new ChitChatRequest(message.ConversationId, message.Text))
        };

        var result = await resultTask.WaitAsync(CancellationToken.None);
        
        _logger.LogDebug("{@Result}", result);
        switch (result.ResponseType)
        {
            case ResponseType.Text:
                await _mediator.Send(new BotTextMessageRequest(message.ConversationId, result.UserMessageText ?? "",
                    result.Type == ResultType.Error));
                break;
            case ResponseType.Animation:
                await _mediator.Send(new BotGifMessageRequest(message.ConversationId, result.UserMessageText!));
                break;
            case ResponseType.Media:
            case ResponseType.Menu:
                throw new NotImplementedException($"Response Type of {result.ResponseType} has not been implemented");
            default:
                throw new InvalidOperationException($"Unknown bot response type: {result.ResponseType}");
        }
    }

    public bool AddUser(long conversationId, string firstName, string? lastName)
    {
        return _usersByConversationId.TryAdd(conversationId, new User(Guid.NewGuid(), firstName, lastName, conversationId));
    }
    
    private void AddMessageEntry(long conversationId, string message, MessageDirection direction)
    {
        if (!_usersByConversationId.ContainsKey(conversationId))
        {
            throw new InvalidOperationException("Unknown User/Conversation");
        }

        var user = _usersByConversationId[conversationId];
        
        if (!_conversationsById.ContainsKey(conversationId))
        {
            _conversationsById[conversationId] = new FixedLengthList<ConversationEntry>(10);
        }

        var list = _conversationsById[conversationId];
        list.Add(new ConversationEntry(conversationId, user, message, direction, DateTimeOffset.UtcNow));
    }
}
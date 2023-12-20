using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moneo.Chat.BotRequests;
using Moneo.Chat.Commands;
using Moneo.Chat.Models;
using Moneo.Chat.UserRequests;
using Moneo.Chat.Workflows.Chitchat;
using Moneo.Chat.Workflows.CreateTask;
using Moneo.Core;

namespace Moneo.Chat;

public class ConversationManager : IConversationManager
{
    private readonly Dictionary<long, FixedLengthList<ConversationEntry>> _conversationsById = new ();
    private readonly Dictionary<long, User> _usersByConversationId = new();
    private readonly Dictionary<long, ConversationState> _states = new();
    private readonly IMediator _mediator;
    private readonly ILogger<ConversationManager> _logger;

    public ConversationManager(IMediator mediator, ILogger<ConversationManager> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public void SetConversationState(long conversationId, ConversationState state)
    {
        _states[conversationId] = state;
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

        var result = ContainsSlashCommand(message.Text)
            ? await ProcessCommandAsync(message.ConversationId, message.Text)
            : await HandleNonCommandMessageAsync(message.ConversationId, message.Text);

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
                throw new NotImplementedException($"Response Type of {result.ResponseType} has not been implemented");
            default:
                throw new InvalidOperationException($"Unknown bot response type: {result.ResponseType}");
        }
    }

    public bool AddUser(long conversationId, string firstName, string? lastName)
    {
        return _usersByConversationId.TryAdd(conversationId, new User(Guid.NewGuid(), firstName, lastName, conversationId));
    }
    
    private static bool ContainsSlashCommand(string input)
    {
        // Define a regular expression pattern for a slash command
        const string pattern = @"^\/\w+"; // Assumes the slash command starts with a slash and is followed by word characters

        // Check if the input string matches the pattern
        return Regex.IsMatch(input, pattern);
    }

    private Task<MoneoCommandResult> HandleNonCommandMessageAsync(long conversationId, string text)
    {
        if (!_states.TryGetValue(conversationId, out var state))
        {
            state = ConversationState.Waiting;
            _states[conversationId] = state;
        }

        return state switch
        {
            ConversationState.CreateTask => _mediator.Send(new CreateTaskContinuationRequest(conversationId, text)),
            _ => _mediator.Send(new ChitChatRequest(conversationId, text))
        };
    }

    private async Task<MoneoCommandResult> ProcessCommandAsync(long conversationId, string text)
    {
        var parts = text.Split(' ');
        var cmd = parts.First(p => p.StartsWith("/")).ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        // this should really be replaced with dynamically loading the command keys and a func from the assembly
        return cmd switch
        {
            CompleteTaskRequest.CommandKey => await _mediator.Send(new CompleteTaskRequest(conversationId, args)),
            SkipTaskRequest.CommandKey => await _mediator.Send(new SkipTaskRequest(conversationId, args)),
            CreateTaskRequest.CommandKey => await _mediator.Send(new CreateTaskRequest(conversationId, args)),
            _ => new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = $"Unknown command: {cmd}"
            }
        };
    }
    
    private void AddMessageEntry(long conversationId, string message, MessageDirection direction)
    {
        if (!_usersByConversationId.TryGetValue(conversationId, out var user))
        {
            throw new InvalidOperationException("Unknown User/Conversation");
        }

        if (!_conversationsById.TryGetValue(conversationId, out var conversationHistory))
        {
            conversationHistory = new FixedLengthList<ConversationEntry>(10);
            _conversationsById[conversationId] = conversationHistory;
        }
        
        conversationHistory.Add(new ConversationEntry(conversationId, user, message, direction, DateTimeOffset.UtcNow));
    }
}
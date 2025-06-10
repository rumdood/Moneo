using System.Text;
using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;
using RadioFreeBot.Features.AddSongToPlaylist;
using RadioFreeBot.Models;
using RadioFreeBot.ResourceAccess;

namespace RadioFreeBot.Features.RemoveSongFromPlaylist;

public interface IRemoveSongFromPlaylistWorkflowManager : IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext context, string songName,
        CancellationToken cancellationToken = default);
}

internal class RemoveSongFromPlaylistStateRepository : IWorkflowStateMachineRepository<RemoveSongFromPlaylistState>
{
    private readonly Dictionary<ConversationUserKey, IWorkflowStateMachine<RemoveSongFromPlaylistState>> _chatStates =
        new();

    public bool ContainsKey(ConversationUserKey key) => _chatStates.ContainsKey(key);

    public void Add(ConversationUserKey key, IWorkflowStateMachine<RemoveSongFromPlaylistState> stateMachine)
        => _chatStates.Add(key, stateMachine);

    public bool TryGetValue(ConversationUserKey key,
        out IWorkflowStateMachine<RemoveSongFromPlaylistState> stateMachine)
        => _chatStates.TryGetValue(key, out stateMachine);

    public void Remove(ConversationUserKey key) => _chatStates.Remove(key);
}

[MoneoWorkflow]
internal partial class RemoveSongFromPlaylistWorkflowManager : WorkflowManagerBase, IRemoveSongFromPlaylistWorkflowManager
{
    private readonly ILogger<RemoveSongFromPlaylistWorkflowManager> _logger;
    private readonly IYouTubeMusicProxyClient _youTubeMusicProxyClient;
    private readonly RadioFreeDbContext _context;
    private readonly RemoveSongFromPlaylistStateRepository _workflowStateMachineRepository = new();

    private readonly
        Dictionary<RemoveSongFromPlaylistState, Func<RemoveSongFromPlaylistStateMachine, string, CancellationToken,
            Task<ResponseHandleResult>>> _responseHandlers;

    private readonly
        Dictionary<RemoveSongFromPlaylistState, Func<RemoveSongFromPlaylistStateMachine,
            MoneoCommandResult>> _responseStore = new();

    public RemoveSongFromPlaylistWorkflowManager(
        IMediator mediator,
        ILogger<RemoveSongFromPlaylistWorkflowManager> logger,
        IYouTubeMusicProxyClient proxyClient,
        RadioFreeDbContext dbContext) : base(mediator)
    {
        _logger = logger;
        _youTubeMusicProxyClient = proxyClient;
        _context = dbContext;

        _responseHandlers =
            new Dictionary<RemoveSongFromPlaylistState, Func<RemoveSongFromPlaylistStateMachine, string,
                CancellationToken, Task<ResponseHandleResult>>>
            {
                { RemoveSongFromPlaylistState.Search, HandleSearch },
                { RemoveSongFromPlaylistState.Select, HandleSelect },
                { RemoveSongFromPlaylistState.Confirm, HandleConfirm },
            };
        
        InitializeResponses();
    }

    private async Task<ResponseHandleResult> HandleSearch(
        RemoveSongFromPlaylistStateMachine machine,
        string songName,
        CancellationToken cancellationToken = default)
    {
        if (machine.CurrentState != RemoveSongFromPlaylistState.Search)
        {
            return new ResponseHandleResult(false,
                "I'm very confused apparently. I thought we were searching for a song.");
        }

        // search for the song on the playlist
        var searchResult =
            await _youTubeMusicProxyClient.FindSongOnPlaylistAsync(
                machine.SongName,
                machine.PlaylistId,
                cancellationToken);

        if (!searchResult.IsSuccess || searchResult.Data is null || !searchResult.Data.Any())
        {
            return new ResponseHandleResult(false, searchResult.Message);
        }

        return new ResponseHandleResult(true);
    }

    private static Task<ResponseHandleResult> HandleSelect(RemoveSongFromPlaylistStateMachine machine, string userInput,
        CancellationToken cancellationToken = default)
    {
        if (machine.CurrentState != RemoveSongFromPlaylistState.Select)
        {
            return Task.FromResult(new ResponseHandleResult(false,
                "I'm very confused apparently. I thought we were selecting a song."));
        }

        // the user input should either be a number, or a choice in the format of "1. Song Title by Artist Name" and we want the number
        // if the user input is a number, we can use that to select the song
        // if the user input is a choice, we can parse the number from the choice
        // Try to parse the first number from the input
        var trimmedInput = userInput.Trim();

        // Match a number at the start of the string (e.g., "2" or "2. Song Title")
        var match = SelectionIndexRegex().Match(trimmedInput);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var selectedIndex))
        {
            // Convert to zero-based index
            selectedIndex -= 1;
            if (selectedIndex >= 0)
            {
                if (selectedIndex < machine.SongOptions.Count)
                {
                    machine.SelectSong(machine.SongOptions[selectedIndex].Id);
                    return Task.FromResult(new ResponseHandleResult(true));
                }
                if (selectedIndex == machine.SongOptions.Count)
                {
                    // User selected the cancel option
                    machine.Cancel();
                    return Task.FromResult(new ResponseHandleResult(true));
                }
            }
        }

        return Task.FromResult(new ResponseHandleResult(false,
            "Please select a valid option by entering the corresponding number."));
    }

    private async Task<ResponseHandleResult> HandleConfirm(RemoveSongFromPlaylistStateMachine machine, string userInput,
        CancellationToken cancellationToken = default)
    {
        if (machine.CurrentState != RemoveSongFromPlaylistState.Confirm)
        {
            return new ResponseHandleResult(false,
                "I'm very confused apparently. I thought we were confirming a song selection.");
        }

        if (userInput.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            // try to remove the song from the playlist
            var result =
                await _youTubeMusicProxyClient.RemoveSongFromPlaylistAsync(machine.PlaylistId, machine.SongId,
                    cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to remove song {SongId} from playlist {PlaylistId}: {ErrorMessage}",
                    machine.SongId, machine.PlaylistId, result.Message);
                return new ResponseHandleResult(false, result.Message);
            }

            _logger.LogInformation("Successfully removed song {SongId} from playlist {PlaylistId}", machine.SongId,
                machine.PlaylistId);
            return new ResponseHandleResult(true);
        }
        
        if (userInput.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("User cancelled the removal of song {SongId} from playlist {PlaylistId}",
                machine.SongId, machine.PlaylistId);
            machine.Cancel();
            return new ResponseHandleResult(true);
        }
        
        return new ResponseHandleResult(false,
            "Please respond with 'yes' or 'no'. I don't understand your response.");
    }

    private async Task<MoneoCommandResult> ContinueWorkflowInternalAsync(
        CommandContext context,
        RemoveSongFromPlaylistStateMachine machine,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Continuing RemoveSongFromPlaylist workflow for user input: {UserInput} in conversation ID: {ConversationId} for User: {User}",
            userInput, context.ConversationId, context.User?.Username);
        if (_responseHandlers.TryGetValue(machine.CurrentState, out var handler))
        {
            var result = await handler.Invoke(machine, userInput, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Error in workflow for conversation ID: {ConversationId} and user ID: {UserId}: {ErrorMessage}",
                    context.ConversationId, context.User?.Id, result.FailureMessage);
                CleanupStateMachine(context.GenerateConversationUserKey());
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = result.FailureMessage ?? "An error occurred while processing your request."
                };
            }
        }

        var response = GetResponseToState(machine);

        while (response is null 
               && machine.CurrentState != RemoveSongFromPlaylistState.Complete 
               && machine.CurrentState != RemoveSongFromPlaylistState.Cancel)
        {
            response = GetResponseToState(machine);
        }

        if (machine.CurrentState is RemoveSongFromPlaylistState.Complete or RemoveSongFromPlaylistState.Cancel)
        {
            await CompleteWorkflowAsync(context, cancellationToken);
        }

        if (response is null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    "I don't know what to do next. I have failed you and my creator and all I can be is sorry."
            };
        }

        return response;
    }

    private async Task CompleteWorkflowAsync(CommandContext context, CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new RemoveSongFromPlaylistWorkflowCompletedEvent(context), cancellationToken);
        var conversationUserKey = context.GenerateConversationUserKey();
        CleanupStateMachine(conversationUserKey);
    }
    
    private static MoneoCommandResult GetResponseForSelectingSong(RemoveSongFromPlaylistStateMachine machine)
    {
        if (machine.SongOptions is null || machine.SongOptions.Count == 0)
        {
            machine.Cancel();
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.WorkflowCompleted,
                UserMessageText = "I couldn't find any songs to remove from the playlist. Not removing any songs."
            };
        }
        
        var optionStrings = machine.SongOptions.Select((song, index) =>
            $"{index + 1}. {song.Title} by {song.Artist}").ToHashSet();
        
        var cancelOption = $"{optionStrings.Count + 1}. Cancel";
        optionStrings.Add(cancelOption);
        
        return new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "I found a few possible songs to remove. Tell me the number of the song you want to remove:",
            MenuOptions = optionStrings
        };
    }

    private void InitializeResponses()
    {
        _responseStore[RemoveSongFromPlaylistState.Select] = GetResponseForSelectingSong;

        _responseStore[RemoveSongFromPlaylistState.Confirm] = _ => new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "Are you sure you want to remove this song from the playlist? (yes/no)"
        };

        _responseStore[RemoveSongFromPlaylistState.Complete] = machine => new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = $"I've removed '{machine.SongName}' from the playlist."
        };
        
        _responseStore[RemoveSongFromPlaylistState.Cancel] = _ => new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText = "Got it. Not removing any songs from the playlist."
        };
    }

    private MoneoCommandResult? GetResponseToState(RemoveSongFromPlaylistStateMachine machine)
    {
        return !_responseStore.TryGetValue(machine.GoToNext(), out var result) ? null : result(machine);
    }

    public Task<MoneoCommandResult> ContinueWorkflowAsync(CommandContext context, string userInput,
        CancellationToken cancellationToken = default)
    {
        // Check if the workflow state machine exists for the conversation user key
        var conversationUserKey = context.GenerateConversationUserKey();
        if (!_workflowStateMachineRepository.TryGetValue(conversationUserKey, out var stateMachine))
        {
            _logger.LogWarning(
                "No workflow state machine found for conversation ID: {ConversationId} and user ID: {UserId}",
                context.ConversationId, context.User?.Id);
            return Task.FromResult(new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    "I...don't seem to be doing anything with removing songs from playlists at the moment."
            });
        }

        if (stateMachine is not RemoveSongFromPlaylistStateMachine removeSongStateMachine)
        {
            _logger.LogError(
                "Workflow state machine for conversation ID: {ConversationId} and user ID: {UserId} is not of type RemoveSongFromPlaylistStateMachine",
                context.ConversationId, context.User?.Id);
            return Task.FromResult(new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "An error occurred while processing your request. Please try again."
            });
        }

        _logger.LogDebug("Continuing workflow for conversation ID: {ConversationId} and user ID: {UserId}",
            context.ConversationId, context.User?.Id);
        return ContinueWorkflowInternalAsync(context, removeSongStateMachine, userInput, cancellationToken);
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext context, string songName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Starting RemoveSongFromPlaylist workflow for song: {SongName} in conversation ID: {ConversationId} for User: {User}",
            songName, context.ConversationId, context.User?.Username);

        // get the conversation user key
        var conversationUserKey = context.GenerateConversationUserKey();

        // check if the workflow already exists
        if (_workflowStateMachineRepository.ContainsKey(conversationUserKey))
        {
            _logger.LogError("Workflow already exists for conversation ID: {ConversationId} and user ID: {UserId}",
                context.ConversationId, context.User?.Id);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    "A workflow is already in progress for this conversation. Please complete it before starting a new one."
            };
        }

        if (string.IsNullOrEmpty(songName))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "Song Name cannot be empty."
            };
        }

        // get the playlist for the conversation
        var playlist = _context.Playlists.FirstOrDefault(pl => pl.ConversationId == context.ConversationId);

        if (playlist == null)
        {
            _logger.LogWarning("No playlist found for conversation ID: {ConversationId}", context.ConversationId);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "No playlist found for this conversation. Contact the bot owner to create one."
            };
        }

        _logger.LogDebug(
            "Creating new workflow state machine for conversation ID: {ConversationId} and user ID: {UserId}",
            context.ConversationId, context.User?.Id);
        // create a new workflow state machine
        var state = new RemoveSongFromPlaylistStateMachine(playlist.ExternalId, songName);

        // add the workflow state machine to the repository
        _workflowStateMachineRepository.Add(conversationUserKey, state);
        
        await Mediator.Send(
            new RemoveSongFromPlaylistWorkflowStartedEvent(context), cancellationToken);

        // move from the "start" state
        state.GoToNext();

        _logger.LogDebug("Starting workflow for conversation ID: {ConversationId} and user ID: {UserId}",
            context.ConversationId, context.User?.Id);

        // search for the song on the playlist
        var searchResult =
            await _youTubeMusicProxyClient.FindSongOnPlaylistAsync(state.SongName, playlist.ExternalId,
                cancellationToken);

        if (!searchResult.IsSuccess || searchResult.Data is null || !searchResult.Data.Any())
        {
            _logger.LogError("Failed to find song {SongName} on playlist {PlaylistId}: {ErrorMessage}", state.SongName,
                playlist.ExternalId, searchResult.Message);
            CleanupStateMachine(conversationUserKey);
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText =
                    $"I couldn't find the song \"{state.SongName}\" on the playlist. Please check the song name and try again."
            };
        }

        // if we found the song, we can continue the workflow
        _logger.LogDebug("Found {Result} song(s) for name {SongName} on playlist {PlaylistId}", searchResult.Data.Count,
            state.SongName, playlist.ExternalId);

        if (searchResult.Data.Count == 1)
        {
            // we found exactly one song, so we can skip the selection step and go to confirmation
            _logger.LogDebug(
                "Only one song found for name {SongName} on playlist {PlaylistId}, skipping selection step",
                state.SongName, playlist.ExternalId);
            state.SelectSong(searchResult.Data[0].Id);
        }
        else
        {
            state.SetOptions(searchResult.Data);
            _logger.LogDebug("Multiple songs found for name {SongName} on playlist {PlaylistId}, user will select one",
                state.SongName, playlist.ExternalId);
        }

        return await ContinueWorkflowInternalAsync(context, state, songName, cancellationToken);
    }

    private void CleanupStateMachine(ConversationUserKey conversationUserKey)
    {
        if (_workflowStateMachineRepository.ContainsKey(conversationUserKey))
        {
            _logger.LogDebug(
                "Removing workflow state machine for conversation ID: {ConversationId} and user ID: {UserId}",
                conversationUserKey.ConversationId, conversationUserKey.UserId);
            _workflowStateMachineRepository.Remove(conversationUserKey);
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^(\d+)")]
    private static partial System.Text.RegularExpressions.Regex SelectionIndexRegex();
}
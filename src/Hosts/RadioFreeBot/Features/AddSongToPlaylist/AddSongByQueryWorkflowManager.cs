using MediatR;
using Moneo.Chat;
using Moneo.Chat.Commands;
using Moneo.Chat.Workflows;

namespace RadioFreeBot.Features.AddSongToPlaylist;

internal record struct ResponseHandleResult(bool Success, string? FailureMessage = null);

public interface IAddSongByQueryWorkflowManager : IWorkflowManagerWithContinuation
{
    Task<MoneoCommandResult> StartWorkflowAsync(CommandContext context, string playlistId, string userInput, CancellationToken cancellationToken = default);
}

[MoneoWorkflow]
public class AddSongByQueryWorkflowManager : WorkflowManagerBase, IAddSongByQueryWorkflowManager
{
    private readonly IYouTubeMusicProxyClient _client;
    private readonly Dictionary<long, AddSongStateMachine> _chatStates = new();
    private readonly Dictionary<AddSongStates, Func<AddSongStateMachine, MoneoCommandResult>> _responseStore = new();
    private readonly
        Dictionary<AddSongStates, Func<AddSongStateMachine, string, CancellationToken, Task<ResponseHandleResult>>>
        _responseHandlers = new();

    private async Task<ResponseHandleResult> HandleInitialSearch(AddSongStateMachine stateMachine,
        string userInput, CancellationToken cancellationToken = default)
    {
        // the request starts with a song name, we will go search for the song, confirm we have found the song the user wants, and then add it
        var searchResult = await _client.FindSongAsync(userInput, cancellationToken);
        
        if (!searchResult.IsSuccess || searchResult.Data?.Count == 0)
        {
            return new ResponseHandleResult(false, "I couldn't find that song. Please try again.");
        }
        
        // we have a result, so we can add the result to the state machine
        var songOptions = searchResult.Data!;
        stateMachine.SetSongOptions(songOptions);

        return new ResponseHandleResult(true);
    }

    private static Task<ResponseHandleResult> HandleSongSelectionConfirmation(AddSongStateMachine stateMachine, string userInput, CancellationToken cancellationToken = default)
    {
        if (stateMachine.CurrentState != AddSongStates.InitialSearch)
        {
            return Task.FromResult(new ResponseHandleResult(false, "I'm very confused apparently. I thought we were selecting a song."));
        }
        
        if (userInput.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            stateMachine.ConfirmSelection();
            return Task.FromResult(new ResponseHandleResult(true));
        }
        
        if (userInput.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ResponseHandleResult(true));
        }
        
        return Task.FromResult(new ResponseHandleResult(false, "Please respond with 'yes' or 'no'."));
    }

    private void InitializeResponseHandlers()
    {
        _responseHandlers[AddSongStates.InitialSearch] = HandleSongSelectionConfirmation;
    }

    private void InitializeResponses()
    {
        _responseStore[AddSongStates.Start] = _ => new MoneoCommandResult { 
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "What song do you want to add to your playlist?"
        };
        _responseStore[AddSongStates.Select] = (stateMachine) => new MoneoCommandResult
        {
            ResponseType = ResponseType.Menu,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = @"I found the following songs. Which one do you want to add to your playlist?",
            MenuOptions = stateMachine.SongOptions.Select(song => $"{song.Title} ({song.Album} by {song.Artist})")
                .ToHashSet()
        };
        _responseStore[AddSongStates.Select] = (stateMachine) => new MoneoCommandResult
        {
            ResponseType = ResponseType.Text,
            Type = ResultType.WorkflowCompleted,
            UserMessageText =
                $"I added \"{stateMachine.CurrentSelection!.Title}\" by {stateMachine.CurrentSelection.Artist} to your playlist!"
        };
        _responseStore[AddSongStates.SecondarySearch] = _ => new MoneoCommandResult { 
            ResponseType = ResponseType.Text,
            Type = ResultType.NeedMoreInfo,
            UserMessageText = "What song do you want to add to your playlist?"
        };
    }

    private MoneoCommandResult? GetResponseToState(AddSongStates state, AddSongStateMachine machine)
    {
        return !_responseStore.TryGetValue(state, out var result) ? null : result(machine);
    }
    
    public AddSongByQueryWorkflowManager(IMediator mediator, IYouTubeMusicProxyClient client) : base(mediator)
    {
        _client = client;
        InitializeResponseHandlers();
        InitializeResponses();
    }

    public async Task<MoneoCommandResult> StartWorkflowAsync(CommandContext context, string playlistId, string? userInput, CancellationToken cancellationToken = default)
    {
        // check if the user is already in a workflow
        if (_chatStates.ContainsKey(context.ConversationId))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "You are already in a workflow. Please finish that one before starting a new one."
            };
        }

        await Mediator.Send(new AddSongToPlaylistWorkflowStartedEvent(context.ConversationId, context.User?.Id ?? 0), cancellationToken);
        var machine = _chatStates[context.ConversationId] = new AddSongStateMachine(playlistId);

        if (!string.IsNullOrEmpty(userInput))
        {
            // apparently they gave us a song name, so we'll skip asking them which song they want
            machine.GoToNext();
        }
        
        return await ContinueWorkflowAsync(context, userInput ?? "", cancellationToken);
    }
    
    private async Task<ResponseHandleResult> SelectAndAddSongToPlaylistAsync(AddSongStateMachine machine, CancellationToken cancellationToken = default)
    {
        machine.GoToNext();
        if (machine.CurrentSelection is null)
        {
            return new ResponseHandleResult(false,
                "I...don't seem to be doing anything with adding songs to playlists at the moment.");
        }

        try
        {
            var isSongOnPlaylist = await _client.GetSongFromPlaylistAsync(machine.PlaylistId,
                machine.CurrentSelection.Id, cancellationToken);

            if (isSongOnPlaylist is { IsSuccess: true, Data: not null })
            {
                return new ResponseHandleResult(false, "That song is already in your playlist.");
            }

            var addResult =
                await _client.AddSongToPlaylistAsync(machine.PlaylistId, machine.CurrentSelection.Id,
                    cancellationToken);

            return !addResult.IsSuccess 
                ? new ResponseHandleResult(false, "I couldn't add that song to your playlist.") 
                : new ResponseHandleResult(true);
        }
        catch (Exception ex)
        {
            return new ResponseHandleResult(false,
                "I couldn't add that song to your playlist because something went wrong.");
        }
    }

    private async Task CompleteWorkflowAsync(CommandContext context, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AddSongToPlaylistWorkflowCompletedEvent(context.ConversationId, context.User?.Id ?? 0), cancellationToken);
        _chatStates.Remove(context.ConversationId);
    }

    public async Task<MoneoCommandResult> ContinueWorkflowAsync(CommandContext context, string userInput, CancellationToken cancellationToken = default)
    {
        if (!_chatStates.TryGetValue(context.ConversationId, out var machine))
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "I...don't seem to be doing anything with adding songs to playlists at the moment."
            };
        }

        if (_responseHandlers.TryGetValue(machine.CurrentState, out var handler))
        {
            var result = await handler.Invoke(machine, userInput, cancellationToken);

            if (!result.Success)
            {
                return new MoneoCommandResult
                {
                    ResponseType = ResponseType.Text,
                    Type = ResultType.Error,
                    UserMessageText = result.FailureMessage ?? "I don't know what to do here."
                };
            }
        }

        var response = GetResponseToState(machine.GoToNext(), machine);

        while (response is null && machine.CurrentState != AddSongStates.Complete)
        {
            response = GetResponseToState(machine.GoToNext(), machine);
        }

        if (machine.CurrentState == AddSongStates.Complete)
        {
            await CompleteWorkflowAsync(context, cancellationToken);
        }

        if (response is null)
        {
            return new MoneoCommandResult
            {
                ResponseType = ResponseType.Text,
                Type = ResultType.Error,
                UserMessageText = "This is apocalyptically bad"
            };
        }

        return response;
    }
}
using Moneo.Chat.Workflows;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.AddSongToPlaylist;

internal enum AddSongStates
{
    Start,
    InitialSearch,
    SecondarySearch,
    Select,
    AddToPlaylist,
    Complete
}

internal class AddSongStateMachine : IWorkflowStateMachine<AddSongStates>
{
    private readonly Dictionary<string, SongItem> _songOptions = [];
    
    public AddSongStateMachine(string playlistId, SongItem? item = null)
    {
        PlaylistId = playlistId;
        CurrentSelection = item;

        if (item is not null)
        {
            CurrentState = AddSongStates.InitialSearch;
        }
    }
    
    public AddSongStates CurrentState { get; private set; } = AddSongStates.Start;
    public string PlaylistId { get; init; }
    public SongItem? CurrentSelection { get; set; }
    public IReadOnlyList<SongItem> SongOptions => _songOptions.Values.ToList();
    public bool IsConfirmed { get; private set; } = false;
    
    public void ConfirmSelection() => IsConfirmed = true;

    public void SetSongOptions(IEnumerable<SongItem> songOptions)
    {
        _songOptions.Clear();
        foreach (var song in songOptions)
        {
            _songOptions[song.Id] = song;
        }
    }
    
    public AddSongStates GoToNext()
    {
        switch (CurrentState)
        {
            case AddSongStates.Start:
                CurrentState = AddSongStates.InitialSearch;
                break;
            case AddSongStates.InitialSearch:
            case AddSongStates.SecondarySearch:
                CurrentState = AddSongStates.Select;
                break;
            case AddSongStates.Select:
                CurrentState = IsConfirmed ? AddSongStates.AddToPlaylist : AddSongStates.SecondarySearch;
                break;
            case AddSongStates.AddToPlaylist:
                CurrentState = AddSongStates.Complete;
                break;
            case AddSongStates.Complete:
            default:
                break;
        }
        
        return CurrentState;
    }
}
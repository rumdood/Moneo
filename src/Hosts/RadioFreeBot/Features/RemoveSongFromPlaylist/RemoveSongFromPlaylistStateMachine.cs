using Moneo.Chat.Workflows;
using RadioFreeBot.Models;

namespace RadioFreeBot.Features.RemoveSongFromPlaylist;

public enum RemoveSongFromPlaylistState
{
    Start,
    Search,
    Select,
    Confirm,
    Complete,
    Cancel
}

public class RemoveSongFromPlaylistStateMachine : IWorkflowStateMachine<RemoveSongFromPlaylistState>
{
    public string PlaylistId { get; init; }
    public string SongName { get; init; }
    public string? SongId { get; private set; }
    public List<SongItem> SongOptions { get; } = [];
    public bool IsSongRemoved { get; }
    
    public RemoveSongFromPlaylistStateMachine(string playlistId, string songName)
    {
        PlaylistId = playlistId;
        SongName = songName;
        CurrentState = RemoveSongFromPlaylistState.Start;
    }
    
    public RemoveSongFromPlaylistState CurrentState { get; private set; }

    public void SelectSong(string songId)
    {
        var song = SongOptions.FirstOrDefault(s => s.Id == songId);
        if (song is null)
        {
            throw new ArgumentException($"Song with ID {songId} not found in options.");
        }
        SongId = songId;
    }

    public void SetOptions(IEnumerable<SongItem> songOptions)
    {
        SongOptions.Clear();
        SongOptions.AddRange(songOptions);
    }

    public void Cancel()
    {
        CurrentState = RemoveSongFromPlaylistState.Cancel;
    }
    
    public RemoveSongFromPlaylistState GoToNext()
    {
        switch (CurrentState)
        {
            case RemoveSongFromPlaylistState.Start:
                CurrentState = RemoveSongFromPlaylistState.Search;
                break;
            case RemoveSongFromPlaylistState.Search:
                if (SongOptions.Count == 0)
                {
                    CurrentState = RemoveSongFromPlaylistState.Complete;
                }
                else if (SongOptions.Count == 1)
                {
                    SongId = SongOptions[0].Id;
                    CurrentState = RemoveSongFromPlaylistState.Confirm;
                }
                else
                {
                    CurrentState = RemoveSongFromPlaylistState.Select;
                }

                break;
            case RemoveSongFromPlaylistState.Select:
                if (SongId is not null)
                {
                    CurrentState = RemoveSongFromPlaylistState.Confirm;
                }
                else
                {
                    CurrentState = RemoveSongFromPlaylistState.Search;
                }
                break;
            case RemoveSongFromPlaylistState.Confirm:
                if (SongId is null)
                {
                    throw new NullReferenceException("SongId is null");
                }
                
                CurrentState = RemoveSongFromPlaylistState.Complete;
                break;
            case RemoveSongFromPlaylistState.Cancel:
                break;
            default:
                throw new InvalidOperationException($"Invalid state: {CurrentState}");
        }
        
        return CurrentState;
    }
}
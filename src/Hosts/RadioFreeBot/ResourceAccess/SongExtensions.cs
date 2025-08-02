using Microsoft.EntityFrameworkCore;
using Moneo.Common;
using RadioFreeBot.Models;
using RadioFreeBot.ResourceAccess.Entities;

namespace RadioFreeBot.ResourceAccess;

public static class SongExtensions
{
    private class BatchContext
    {
        public HashSet<string> SongExternalIds { get; } = [];
        public Dictionary<string, Song> SongsByExternalId { get; } = new();
        public Dictionary<string, Artist> ArtistsByName { get; } = new();
        public Dictionary<string, Album> AlbumsByNameAndArist { get; } = new();

        public BatchContext(IEnumerable<SongItem> songItems)
        {
            SongExternalIds = songItems.Select(s => s.Id).ToHashSet();
        }
    }
    
    private static string GetAlbumKey(Album album)
    {
        return album.Name + "/" + string.Join('/', album.Artists.Select(x => x.Name));
    }
    
    public static async Task<MoneoResult<Dictionary<string, Song>>> GetOrCreateSongsAsync(
        this RadioFreeDbContext dbContext,
        IReadOnlyList<SongItem> songItems,
        CancellationToken cancellationToken = default)
    {
        var context = new BatchContext(songItems);

        try
        {
            var existingSongs = await dbContext.Songs
            .Include(s => s.Artists)
            .Include(s => s.Albums)
            .Include(s => s.PlaylistSongs)
            .Where(s => s.ExternalId != null && context.SongExternalIds.Contains(s.ExternalId))
            .ToListAsync(cancellationToken);
        
        foreach (var song in existingSongs)
        {
            if (song.ExternalId is null)
            {
                continue; // Skip songs without an external ID
            }

            context.SongsByExternalId[song.ExternalId] = song;

            foreach (var artist in song.Artists)
            {
                context.ArtistsByName[artist.Name] = artist;
            }

            foreach (var album in song.Albums)
            {
                context.AlbumsByNameAndArist[GetAlbumKey(album)] = album;
            }
        }
        
        foreach (var songItem in songItems)
        {
            if (songItem is null)
            {
                continue; // Skip null items
            }

            if (!context.SongsByExternalId.TryGetValue(songItem.Id, out var song))
            {
                song = new Song(songItem.Title, songItem.Id);
                context.SongsByExternalId[songItem.Id] = song;
                dbContext.Songs.Add(song);
            }
            
            if (!string.IsNullOrEmpty(songItem.Artist))
            {
                if (context.ArtistsByName.TryGetValue(songItem.Artist, out var artist))
                {
                    artist.Songs.Add(song);
                }
                else
                {
                    artist = new Artist(songItem.Artist);
                    context.ArtistsByName[songItem.Artist] = artist;
                    dbContext.Artists.Add(artist);
                    artist.Songs.Add(song);
                }
            }
            
            if (!string.IsNullOrEmpty(songItem.Album))
            {
                var albumKey = GetAlbumKey(new Album(songItem.Album));
                if (context.AlbumsByNameAndArist.TryGetValue(albumKey, out var album))
                {
                    album.Songs.Add(song);
                }
                else
                {
                    album = new Album(songItem.Album);
                    context.AlbumsByNameAndArist[albumKey] = album;
                    dbContext.Albums.Add(album);
                    album.Songs.Add(song);

                    foreach (var artist in song.Artists)
                    {
                        album.Artists.Add(artist);
                    }
                }
            }
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return MoneoResult<Dictionary<string, Song>>.Success(context.SongsByExternalId);
        }
        catch (Exception e)
        {
            return MoneoResult<Dictionary<string, Song>>.Failed(
                "An error occurred while processing the songs.",
                e);
        }
    }
    
    public static async Task<MoneoResult<Song>> GetOrCreateSongAsync(
        this RadioFreeDbContext dbContext,
        SongItem songItem,
        Playlist? playlist = null,
        User? user = null,
        CancellationToken cancellationToken = default)
    {
        if (songItem is null)
        {
            return MoneoResult<Song>.NotFound("Song cannot be null.");
        }

        var existingSong = dbContext.Songs
            .Include(s => s.Artists)
            .Include(s => s.Albums)
            .Include(s => s.PlaylistSongs)
            .FirstOrDefault(s => s.ExternalId == songItem.Id);
        
        if (existingSong != null)
        {
            return MoneoResult<Song>.Success(existingSong);
        }

        existingSong = new Song(songItem.Title, songItem.Id);
        
        if (!string.IsNullOrEmpty(songItem.Artist))
        {
            var existingArtist = dbContext.Artists
                .Include(a => a.Albums.Where(album => album.Name == songItem.Album))
                .FirstOrDefault(a => a.Name == songItem.Artist);
            
            if (existingArtist != null)
            {
                // create the relationship with the existing artist
                existingSong.Artists.Add(existingArtist);

                if (existingArtist.Albums.Count > 0)
                {
                    existingSong.Albums.Add(existingArtist.Albums.First());
                }
            }
            else
            {
                existingArtist = new Artist(songItem.Artist);
                dbContext.Artists.Add(existingArtist);
                existingSong.Artists.Add(existingArtist);
            }
        }
        
        if (!string.IsNullOrEmpty(songItem.Album) && existingSong.Albums.Count == 0)
        {
            var existingAlbum = dbContext.Albums
                .FirstOrDefault(a => a.Name == songItem.Album && a.Artists.Any(artist => artist.Name == songItem.Artist));
            
            if (existingAlbum == null)
            {
                existingAlbum = new Album(songItem.Album);
                dbContext.Albums.Add(existingAlbum);
            }

            existingSong.Albums.Add(existingAlbum);
        }
        
        // If a playlist is provided, create a PlaylistSong entry
        if (playlist != null)
        {
            var playlistSong = new PlaylistSong(playlist, existingSong, DateTime.UtcNow, user?.Id);
            dbContext.PlaylistSongs.Add(playlistSong);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Return the IDs of the song, artist, and album
        return MoneoResult<Song>.Success(existingSong);
    }
}
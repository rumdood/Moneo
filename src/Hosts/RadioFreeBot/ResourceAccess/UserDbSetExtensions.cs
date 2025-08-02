using Microsoft.EntityFrameworkCore;
using RadioFreeBot.Features.AddSongToPlaylist;
using RadioFreeBot.Models;
using RadioFreeBot.ResourceAccess.Entities;

namespace RadioFreeBot.ResourceAccess;

internal static class UserDbSetExtensions
{
    public static async Task<UserWithHistory?> GetUserPlaylistHistoryForTelegramAsync(
        this RadioFreeDbContext context,
        long playlistId,
        long? telegramId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        var result = await context.Users
            .AsNoTracking()
            .Where(u => 
                (telegramId.HasValue && u.TelegramId == telegramId) || 
                (!string.IsNullOrEmpty(userName) && EF.Functions.Like(u.Name, userName)))    
            .Select(u => new
            {
                u.TelegramId,
                u.Name,
                History = context.PlaylistSongs
                    .AsNoTracking()
                    .Where(ps => ps.PlaylistId == playlistId &&
                                 ps.AddedByUserId == u.Id &&
                                 !string.IsNullOrEmpty(ps.Song.ExternalId))
                    .OrderByDescending(ps => ps.AddedAt)
                    .Select(ps => new UserPlaylistHistory(
                        new SongItem(ps.Song.ExternalId!, ps.Song.Name, "", ""),
                        ps.AddedAt,
                        SongHistoryType.Added))
                    .Take(10)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }
        
        return new UserWithHistory(result.TelegramId!.Value, result.Name, result.History);
    }
    
    public static async Task<UserWithHistory> GetOrCreateUserPlaylistHistoryForTelegramAsync(
        this RadioFreeDbContext context,
        long telegramId,
        string name,
        bool includeHistoryFlag = false,
        long? playlistId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);

        if (user == null)
        {
            user = new User(name, telegramId);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!includeHistoryFlag || playlistId == null)
        {
            return new UserWithHistory(user.TelegramId!.Value, user.Name, []);
        }

        var history = await context.PlaylistSongs
            .AsNoTracking()
            .Where(ps => ps.PlaylistId == playlistId.Value && 
                         ps.AddedByUserId == user.Id &&
                         !string.IsNullOrEmpty(ps.Song.ExternalId))
            .OrderByDescending(ps => ps.AddedAt)
            .Take(10)
            .Select(ps => new UserPlaylistHistory(
                new SongItem(ps.Song.ExternalId!, ps.Song.Name, "", ""),
                ps.AddedAt,
                SongHistoryType.Added))
            .ToListAsync(cancellationToken);

        return new UserWithHistory(user.TelegramId!.Value, user.Name, history);
    }
}
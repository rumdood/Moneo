namespace RadioFreeBot.ResourceAccess.Entities;

public class User : EntityBase
{
    public string Name { get; set; }
    public long? TelegramId { get; set; }
    
    public ICollection<PlaylistSong> PlaylistSongs { get; set; }
}
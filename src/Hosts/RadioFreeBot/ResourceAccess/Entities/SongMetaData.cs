namespace RadioFreeBot.ResourceAccess.Entities;

public class SongMetaData : EntityBase
{
    public ICollection<Artist> Artists = new List<Artist>();
    public Album? Album { get; set; }
    TimeSpan Duration { get; set; }
}

public class Artist : EntityBase
{
    public string Name { get; set; }
}

public class Album : EntityBase
{
    public string Name { get; set; }
    public ICollection<Song> Songs = new List<Song>();
    public ICollection<Artist> Artists = new List<Artist>();
}

public class Tag : EntityBase
{
    public string Name { get; set; }
}
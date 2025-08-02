namespace RadioFreeBot.ResourceAccess.Entities;

public class Artist : EntityBase
{
    public string Name { get; set; }
    
    public ICollection<Song> Songs { get; set; } = new List<Song>();
    
    public ICollection<Album> Albums { get; set; } = new List<Album>();

    private Artist()
    {
    }
    
    public Artist(string name)
    {
        Name = name;
    }
}

public class Album : EntityBase
{
    public string Name { get; set; }
    public ICollection<Song> Songs { get; set; } = new List<Song>();
    public ICollection<Artist> Artists { get; set; } = new List<Artist>();

    private Album()
    {
    }

    public Album(string name)
    {
        Name = name;
    }
}

public class Tag : EntityBase
{
    public string Name { get; set; }
}
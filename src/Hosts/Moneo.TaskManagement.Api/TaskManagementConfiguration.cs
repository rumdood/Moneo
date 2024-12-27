namespace Moneo.TaskManagement.Api;

public class MoneoConfiguration
{
    public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.Sqlite;
    public string ConnectionString { get; set; } = "Data Source=moneo.db";
}

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
}
namespace Moneo.TaskManagement.Client.Models;

public class MoneoTaskFilter
{
    public string? TaskId { get; init; }
    public string? SearchString { get; init; }
    public TaskActiveStatusFilter IsActiveFilter { get; init; } = TaskActiveStatusFilter.Undefined;
}

public enum TaskActiveStatusFilter
{
    Undefined,
    Active,
    Inactive
}
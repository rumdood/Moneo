namespace Moneo.TaskManagement.Contracts.Models;

public class PagedList<T> where T : class
{
    public IReadOnlyList<T>? Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
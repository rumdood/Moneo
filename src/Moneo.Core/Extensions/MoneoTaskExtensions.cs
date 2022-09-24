using Moneo.Models;

namespace Moneo.Core;

public static class MoneoTaskExtensions
{
    public static DateTime? GetCompletedOrSkippedOn(this IMoneoTask task)
    {
        return task.CompletedOn ?? task.SkippedOn;
    }
}
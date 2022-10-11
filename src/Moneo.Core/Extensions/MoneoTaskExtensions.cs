using Moneo.Models;

namespace Moneo.Core;

public static class MoneoTaskExtensions
{
    public static DateTime? GetLastCompletedOrSkippedDate(this IMoneoTaskState task)
    {
        var completed = task.LastCompletedOn.FirstOrDefault();
        var skipped = task.LastSkippedOn.FirstOrDefault();

        if (completed.HasValue && skipped.HasValue)
        {
            return completed > skipped ? completed.Value : skipped.Value;
        }

        return completed ?? skipped;
    }
}
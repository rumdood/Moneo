using Moneo.Models;

namespace Moneo.Core;

public static class MoneoTaskExtensions
{
    public static DateTime GetLastCompletedOrSkippedDate(this IMoneoTaskState task)
    {
        if (task.LastCompletedOn.HasValue && task.LastSkippedOn.HasValue)
        {
            return task.LastCompletedOn.Value > task.LastSkippedOn.Value
                ? task.LastCompletedOn.Value
                : task.LastSkippedOn.Value;
        }

        if (task.LastCompletedOn.HasValue && !task.LastSkippedOn.HasValue)
        {
            return task.LastCompletedOn.Value;
        }

        return task.LastSkippedOn ?? default;
    }
}
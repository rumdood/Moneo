using Moneo.Models;

namespace Moneo.Comparison;

internal static class TaskComparer
{
    public static TaskCompareResult CompareTasks(MoneoTaskWithReminders oldTask, MoneoTaskWithReminders newTask)
    {
        return new TaskCompareResult
        (
            new CompareSimpleFieldResult<string> { OriginalValue = oldTask.Name, NewValue = newTask.Name },
            new CompareSimpleFieldResult<string> { OriginalValue = oldTask.Description, NewValue = newTask.Description },
            new CompareSimpleFieldResult<string> { OriginalValue = oldTask.TimeZone, NewValue = newTask.TimeZone },
            new CompareSimpleFieldResult<string> { OriginalValue = oldTask.CompletedMessage, NewValue = newTask.CompletedMessage },
            new CompareBadgerFieldResult { OriginalValue = oldTask.Badger, NewValue = newTask.Badger },
            new CompareRepeaterFieldResult { OriginalValue = oldTask.Repeater, NewValue = newTask.Repeater },
            new CompareCollectionFieldResult<TaskReminder> { OriginalValue = oldTask.Reminders.Values, NewValue = newTask.Reminders.Values }
        );
    }
}
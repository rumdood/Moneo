using System.Text;
using CronExpressionDescriptor;
using Moneo.TaskManagement.Models;

namespace Moneo.Chat;

internal static class MoneoTaskDtoExtensions
{
    internal static string GetTaskSummary(this MoneoTaskDto task)
    {
        var builder = new StringBuilder();
        builder.Append("Task Name: ");
        builder.AppendLine(task.Name);
        builder.Append("Description: ");
        builder.AppendLine(task.Description);

        if (task.Repeater is { } repeater)
        {
            var cronDescription = ExpressionDescriptor.GetDescription(repeater.RepeatCron);
            builder.Append("Repeats: ");
            builder.Append(cronDescription);
            builder.Append("Stops Repeating: ");
            builder.AppendLine(repeater.Expiry.HasValue ? repeater.Expiry.ToString() : "Never");
        }
        else
        {
            builder.AppendLine("** Does Not Repeat **");
        }

        if (task.Badger is { } badger)
        {
            builder.Append("Badger you every ");
            builder.Append(badger.BadgerFrequencyMinutes);
            builder.AppendLine(" minutes if you miss the due date");
        }
        else
        {
            builder.AppendLine("** Do not badger you if you miss your due date");
        }

        if (task.DueDates.Count > 0)
        {
            builder.AppendLine("Task is due on the following dates:");
            foreach (var date in task.DueDates)
            {
                builder.Append("  ");
                builder.AppendLine(date.ToLongDateString());
            }
        }

        return builder.ToString();
    }
}
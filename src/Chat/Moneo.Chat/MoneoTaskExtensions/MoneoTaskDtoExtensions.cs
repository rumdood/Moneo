using System.Text;
using CronExpressionDescriptor;
using Moneo.TaskManagement.Contracts.Models;

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
            var cronDescription = ExpressionDescriptor.GetDescription(repeater.CronExpression);
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
            builder.Append(badger.BadgerFrequencyInMinutes);
            builder.AppendLine(" minutes if you miss the due date");
        }
        else
        {
            builder.AppendLine("** Do not badger you if you miss your due date");
        }

        if (task.DueOn.HasValue)
        {
            builder.AppendLine($"Task is due: {task.DueOn.Value.ToString("dddd, MMMM d, yyyy")}");
        }

        return builder.ToString();
    }
}
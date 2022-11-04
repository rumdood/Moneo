using Moneo.Models;

namespace Moneo.Comparison;

internal sealed record TaskCompareResult
(
    CompareSimpleFieldResult<string> NameField,
    CompareSimpleFieldResult<string> DescriptionField,
    CompareSimpleFieldResult<string> TimeZoneField,
    CompareSimpleFieldResult<string> CompletedMessageField,
    CompareBadgerFieldResult BadgerField,
    CompareRepeaterFieldResult RepeaterField,
    CompareCollectionFieldResult<TaskReminder> RemindersField
);

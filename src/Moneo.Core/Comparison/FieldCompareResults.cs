using Moneo.Models;

namespace Moneo.Comparison;

internal interface ICompareFieldResult
{
    bool IsChanged { get; }
}

internal interface ICompareFieldResult<T> : ICompareFieldResult where T : class
{
    T? OriginalValue { get; }
    T? NewValue { get; }
}

internal sealed class CompareSimpleFieldResult<T> : ICompareFieldResult<T> where T : class
{
    public bool IsChanged { get => OriginalValue == NewValue; }
    public T? OriginalValue { get; set; }
    public T? NewValue { get; set; }
}

internal sealed class CompareCollectionFieldResult<T> : ICompareFieldResult<IEnumerable<T>>
{
    public bool IsChanged
    {
        get => NewValue.SequenceEqual(NewValue);
    }

    public IEnumerable<T> OriginalValue { get; set; }

    public IEnumerable<T> NewValue { get; set; }
}

internal sealed class CompareRepeaterFieldResult : ICompareFieldResult<TaskRepeater>
{
    public bool IsChanged => OriginalValue?.Expiry != NewValue?.Expiry
        || OriginalValue?.RepeatCron.Equals(NewValue?.RepeatCron, StringComparison.Ordinal) == true;

    public TaskRepeater? OriginalValue { get; set; }
    public TaskRepeater? NewValue { get; set; }
}

internal sealed class CompareBadgerFieldResult : ICompareFieldResult<TaskBadger>
{
    public bool IsChanged => OriginalValue?.BadgerFrequencyMinutes != NewValue?.BadgerFrequencyMinutes
        || OriginalValue?.BadgerMessages.SequenceEqual(NewValue?.BadgerMessages ?? Enumerable.Empty<string>()) == true;

    public TaskBadger? OriginalValue { get; set; }
    public TaskBadger? NewValue { get; set; }
}

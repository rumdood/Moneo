using System.Collections;
using Newtonsoft.Json;

namespace Moneo.Core;

[JsonObject]
public sealed class FixedLengthList<T> : IEnumerable<T>
{
    [JsonProperty]
    private T?[] _collection;

    private void ShiftArrayDown(int count = 1)
    {
        var index = _collection.Length;

        while (--index >= count)
        {
            var swapIndex = index - count;
            _collection[index] = _collection[swapIndex];
            _collection[swapIndex] = default;
        }
    }

    [JsonProperty("capacity")]
    public int Capacity { get; init; }

    public T? this[int index] => _collection[index];

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (_collection as IEnumerable<T>).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

    public void Add(T? item)
    {
        ShiftArrayDown();
        _collection[0] = item;
    }

    public void AddRange(IEnumerable<T> items)
    {
        var itemsArray = items as T[] ?? items.ToArray();
        if (itemsArray.Length > _collection.Length)
        {
            throw new InvalidOperationException("Items collection longer than provided capacity");
        }
        
        var lengthDifference = _collection.Length - itemsArray.Length;
        ShiftArrayDown(lengthDifference);
        itemsArray.CopyTo(_collection, 0);
    }

    public void Clear()
    {
        for (var i = 0; i < _collection.Length; i++)
        {
            _collection[i] = default;
        }
    }

    public bool Contains(T item) => _collection.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        for (var i = 0; i < _collection.Length; i++)
        {
            if (_collection[i] is null || !_collection[i]!.Equals(item))
            {
                continue;
            }
            
            _collection[i] = default;
            return true;
        }

        return false;
    }

    [JsonConstructor]
    public FixedLengthList(int maxCapacity)
    {
        Capacity = maxCapacity;
        _collection = new T?[Capacity];
    }

    public FixedLengthList(IEnumerable<T> items, int maxCapacity)
    {
        var itemArray = items.Take(maxCapacity + 1).ToArray();
        
        if (itemArray.Length > maxCapacity)
        {
            throw new InvalidOperationException("Items collection longer than provided capacity");
        }

        Capacity = maxCapacity;
        _collection = new T?[Capacity];
        itemArray.CopyTo(_collection, 0);
    }
}
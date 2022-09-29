using System.Collections;

namespace Moneo.Core;

public sealed class FixedLengthList<T> : ICollection<T>
{
    private T[] _collection;
    private int _maximumCapacity = 0;

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

    private void ResizeCollection()
    {
        var currentSize = _collection.Length;
        var newCollection = new T[currentSize + 1];
        _collection.CopyTo(newCollection, 0);
        _collection = newCollection;
    }

    public int Count => _collection.Length;

    public T this[int index] => _collection[index];

    public IEnumerator<T> GetEnumerator() => (_collection as IEnumerable<T>).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

    public void Add(T item)
    {
        if (_maximumCapacity == 0)
        {
            ResizeCollection();
        }
        
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
            _collection[i] = default(T);
        }
    }

    public bool Contains(T item) => _collection.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        for (var i = 0; i < _collection.Length; i++)
        {
            if (!_collection[i].Equals(item))
            {
                continue;
            }
            
            _collection[i] = default;
            return true;
        }

        return false;
    }
    
    public bool IsReadOnly { get; }

    public FixedLengthList()
    {
        _collection = Array.Empty<T>();
    }

    public FixedLengthList(int maxCapacity)
    {
        _maximumCapacity = maxCapacity;
        _collection = new T[maxCapacity];
    }

    public FixedLengthList(IEnumerable<T> items, int maxCapacity)
    {
        var itemArray = items.Take(maxCapacity + 1).ToArray();
        
        if (itemArray.Length > maxCapacity)
        {
            throw new InvalidOperationException("Items collection longer than provided capacity");
        }

        _maximumCapacity = maxCapacity;

        _collection = new T[maxCapacity];
        itemArray.CopyTo(_collection, 0);
    }
}
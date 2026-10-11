namespace MessageTransit.SignalR.Utils;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// A thread-safe set backed by a <see cref="ConcurrentDictionary{TKey,TValue}" />
/// </summary>
public class ConcurrentHashSet<T> : IDisposable
{
    readonly ConcurrentDictionary<T, byte> _items;

    public ConcurrentHashSet()
    {
        _items = new ConcurrentDictionary<T, byte>();
    }

    public ConcurrentHashSet(IEqualityComparer<T> equalityComparer)
    {
        _items = new ConcurrentDictionary<T, byte>(equalityComparer);
    }

    public int Count => _items.Count;

    public void Dispose()
    {
    }

    public bool Add(T item)
    {
        return _items.TryAdd(item, 0);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Contains(T item)
    {
        return _items.ContainsKey(item);
    }

    public bool Remove(T item)
    {
        return _items.TryRemove(item, out _);
    }

    public T[] ToArray()
    {
        return _items.Keys.ToArray();
    }
}

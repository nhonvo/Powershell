using System;
using System.Collections.Concurrent;

namespace AgyTui;

public sealed class TtlCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime ExpiresAt)> _entries = new();
    private readonly TimeSpan _ttl;

    public TtlCache(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public TValue GetOrCompute(TKey key, Func<TValue> factory)
    {
        if (_entries.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow)
        {
            return e.Value;
        }
        var value = factory();
        _entries[key] = (value, DateTime.UtcNow + _ttl);
        return value;
    }

    public void Invalidate(TKey key)
    {
        _entries.TryRemove(key, out _);
    }

    public void InvalidateAll()
    {
        _entries.Clear();
    }
}

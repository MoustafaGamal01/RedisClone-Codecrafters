namespace RedisSharp;

using System.Collections.Concurrent;

public class Store
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> _data = new();

    public void Set(string key, string value, DateTime? expiresAt = null)
    {
        _data[key] = (value, expiresAt);
    }

    /// <summary>
    /// Returns the value if the key exists and hasn't expired.
    /// Returns null if the key doesn't exist or has expired (and removes it).
    /// </summary>
    public string? Get(string key)
    {
        if (!_data.TryGetValue(key, out var entry))
            return null;

        if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
        {
            _data.TryRemove(key, out _);
            return null;
        }

        return entry.Value;
    }
}

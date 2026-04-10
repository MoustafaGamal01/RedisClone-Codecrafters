namespace RedisSharp;

using System.Collections.Concurrent;

public class Store
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> _stringKeyDic = new();
    private readonly ConcurrentDictionary<List<string>, string> _listKeyDic= new();

    public void Set(string key, string value, DateTime? expiresAt = null)
    {
        _stringKeyDic[key] = (value, expiresAt);
    }

   

    /// <summary>
    /// Returns the value if the key exists and hasn't expired.
    /// Returns null if the key doesn't exist or has expired (and removes it).
    /// </summary>
    public string? Get(string key)
    {
        if (!_stringKeyDic.TryGetValue(key, out var entry))
            return null;

        if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
        {
            _stringKeyDic.TryRemove(key, out _);
            return null;
        }

        return entry.Value;
    }

    public int? RPUSH(List<string> parts)
    {
        // Find the list associated with the key, or create a new one if it doesn't exist
        var key = parts[1];
        var listEntry = _listKeyDic.FirstOrDefault(kv => kv.Value == key);
        
        List<string> list;
       
        if (listEntry.Key == null)
        {
            list = new List<string>();
            _listKeyDic[list] = key;
        }
        else
        {
            // Key already exists
            list = listEntry.Key;
        }

        for (int i = 2; i < parts.Count; i++)
        {
            list.Add(parts[i]);
        }

        return list.Count;
    }

}

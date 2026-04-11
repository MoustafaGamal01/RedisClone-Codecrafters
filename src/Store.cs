namespace RedisSharp;

using System.Collections.Concurrent;

public class Store
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> _stringKeyDic = new();
    private readonly ConcurrentDictionary<List<string>, string> _listKeyDic = new ();
    private readonly ConcurrentDictionary<string, Queue<TaskCompletionSource<string>>> _waiters = new();
    public Store() { }
    //public Store(ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> stringKeyDic,
    //    ConcurrentDictionary<List<string>, string> listKeyDic,
    //    Queue<List<string>> keyQueue)
    //{
    //    _stringKeyDic = stringKeyDic;
    //    _listKeyDic = listKeyDic;
    //    _keyQueue = keyQueue;
    //}

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

    public bool KeySearch(string key)
    {
        return _listKeyDic.Values.Contains(key);
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

    public List<string> LRANGE(List<string> parts, int start, int stop)
    {
        var key = parts[1];
        var listEntry = _listKeyDic.FirstOrDefault(kv => kv.Value == key);

        if (listEntry.Key == null)
            return new List<string>();

        var list = listEntry.Key;
        var listCount = list.Count;
        // Handle negative indices (-1 means last element)
        if (start < 0) start = list.Count + start;
        if (stop < 0) stop = list.Count + stop;
        if (start < 0) start = 0;

        // Clamp to valid range
        stop = Math.Min(stop, listCount - 1);

        if (start > stop || start >= listCount)
            return new List<string>();

        return list.GetRange(start, stop - start + 1);
    }

    public int? LPUSH(List<string> parts)
    {
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
            list = listEntry.Key;
        }

        for (int i = 2; i < parts.Count; i++)
        {
            list.Insert(0, parts[i]);
        }

        return list.Count;
    }

    public int? LLEN(List<string> parts)
    {
        var key = parts[1];

        var list = _listKeyDic.FirstOrDefault(kv => kv.Value == key);

        if (list.Key == null)
            return 0;

        return list.Key.Count;
    }

    public List<string> LPOP(List<string> parts)
    {
        var key = parts[1];
        int partCount = parts.Count;

        var listEntry = _listKeyDic.FirstOrDefault(kv => kv.Value == key);

        if (listEntry.Key == null || listEntry.Key.Count == 0)
            return null;

        var list = listEntry.Key; // all values
        var value = new List<string>(); // values to be removed and returned
        if (partCount == 2)
        {
            value.Add(list[0]);
            list.RemoveAt(0);
        }
        else
        {
            int num = int.Parse(parts[2]);
            if (num > list.Count)
            {

                List<string> temp = new List<string>(list);
                list.Clear();

                return temp;
            }
            value = list.GetRange(0, num); // nums to be removed
            while (num > 0)
            {
                list.RemoveAt(0);
                num--;
            }
        }
        return value;
    }

    public Task<string> BLPOP(string key)
    {
        var tcs = new TaskCompletionSource<string>();
        var queue = _waiters.GetOrAdd(key, _ => new Queue<TaskCompletionSource<string>>());
        lock (queue)
        {
            queue.Enqueue(tcs);
        }
        return tcs.Task; // suspends until SetResult is called
    }

    public bool TryNotifyWaiter(string key, string value)
    {
        if (!_waiters.TryGetValue(key, out var queue)) return false;
        lock (queue)
        {
            if (queue.Count == 0) return false;
            var tcs = queue.Dequeue();
            tcs.SetResult(value); // wakes up the waiting BLPOP client
            return true;
        }
    }
}

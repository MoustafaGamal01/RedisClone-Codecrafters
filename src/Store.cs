namespace RedisSharp;

using System.Collections.Concurrent;

public class Store
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> _stringStore = new();
    private readonly ConcurrentDictionary<string, List<string>> _listStore = new();
    private readonly ConcurrentDictionary<string, Queue<TaskCompletionSource<string>>> _waiters = new();

    public void Set(string key, string value, DateTime? expiresAt = null)
    {
        _stringStore[key] = (value, expiresAt);
    }

    public string? Get(string key)
    {
        if (!_stringStore.TryGetValue(key, out var entry))
            return null;

        if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
        {
            _stringStore.TryRemove(key, out _);
            return null;
        }

        return entry.Value;
    }

    public int? RPUSH(List<string> parts)
    {
        var key = parts[1];
        var list = _listStore.GetOrAdd(key, _ => new List<string>());

        for (int i = 2; i < parts.Count; i++)
            list.Add(parts[i]);

        return list.Count;
    }

    public List<string> LRANGE(List<string> parts, int start, int stop)
    {
        var key = parts[1];

        if (!_listStore.TryGetValue(key, out var list))
            return new List<string>();

        var listCount = list.Count;

        if (start < 0) start = listCount + start;
        if (stop < 0) stop = listCount + stop;
        if (start < 0) start = 0;

        stop = Math.Min(stop, listCount - 1);

        if (start > stop || start >= listCount)
            return new List<string>();

        return list.GetRange(start, stop - start + 1);
    }

    public int? LPUSH(List<string> parts)
    {
        var key = parts[1];
        var list = _listStore.GetOrAdd(key, _ => new List<string>());

        for (int i = 2; i < parts.Count; i++)
            list.Insert(0, parts[i]);

        return list.Count;
    }

    public int? LLEN(List<string> parts)
    {
        var key = parts[1];

        if (!_listStore.TryGetValue(key, out var list))
            return 0;

        return list.Count;
    }

    public List<string>? LPOP(List<string> parts)
    {
        var key = parts[1];

        if (!_listStore.TryGetValue(key, out var list) || list.Count == 0)
            return null;

        var value = new List<string>();

        if (parts.Count == 2)
        {
            value.Add(list[0]);
            list.RemoveAt(0);
        }
        else
        {
            int num = int.Parse(parts[2]);
            if (num > list.Count)
            {
                var temp = new List<string>(list);
                list.Clear();
                return temp;
            }
            value = list.GetRange(0, num);
            list.RemoveRange(0, num);
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
        return tcs.Task;
    }

    public bool TryNotifyWaiter(string key, string value)
    {
        if (!_waiters.TryGetValue(key, out var queue)) return false;
        lock (queue)
        {
            if (queue.Count == 0) return false;
            var tcs = queue.Dequeue();
            tcs.SetResult(value);
            return true;
        }
    }
}
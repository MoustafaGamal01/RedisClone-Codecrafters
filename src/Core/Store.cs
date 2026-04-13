namespace codecrafters_redis.src.Core;

using codecrafters_redis.src.Redis;
using System.Collections.Concurrent;
using codecrafters_redis.src.Core;
using System.Xml.Linq;

public class Store
{

    private readonly ConcurrentDictionary<string, RedisValue> _store = new();
    private readonly ConcurrentDictionary<string, Queue<TaskCompletionSource<string>>> _waiters = new();
    public void Set(string key, string value, DateTime? expiresAt = null)
    {
        _store[key] = new RedisString { type = value, ExpiresAt = expiresAt };
    }

    public string? Get(string key)
    {
        if (!_store.TryGetValue(key, out var entry)) return null;
        if (entry.IsExpired) { _store.TryRemove(key, out _); return null; }
        if (entry is not RedisString str) return null;
        return str.type;
    }

    public int? RPUSH(List<string> parts)
    {
        var key = parts[1];
        var redisList = GetOrCreate<RedisList>(key);

        for (int i = 2; i < parts.Count; i++)
            redisList.Items.Add(parts[i]);

        return redisList.Items.Count;
    }

    public int? LPUSH(List<string> parts)
    {
        var key = parts[1];
        var redisList = GetOrCreate<RedisList>(key);

        for (int i = 2; i < parts.Count; i++)
            redisList.Items.Insert(0, parts[i]);

        return redisList.Items.Count;
    }

    public List<string> LRANGE(List<string> parts, int start, int stop)
    {
        var key = parts[1];

        if (!_store.TryGetValue(key, out var entry) || entry is not RedisList redisList)
            return new List<string>();

        var items = redisList.Items;
        var count = items.Count;

        if (start < 0) start = count + start;
        if (stop < 0) stop = count + stop;
        if (start < 0) start = 0;

        stop = Math.Min(stop, count - 1);

        if (start > stop || start >= count)
            return new List<string>();

        return items.GetRange(start, stop - start + 1);
    }

    public int? LLEN(List<string> parts)
    {
        var key = parts[1];

        if (!_store.TryGetValue(key, out var entry) || entry is not RedisList redisList)
            return 0;

        return redisList.Items.Count;
    }

    public List<string>? LPOP(List<string> parts)
    {
        var key = parts[1];

        if (!_store.TryGetValue(key, out var entry) || entry is not RedisList redisList)
            return null;

        var items = redisList.Items;
        if (items.Count == 0) return null;

        var value = new List<string>();

        if (parts.Count == 2)
        {
            value.Add(items[0]);
            items.RemoveAt(0);
        }
        else
        {
            int num = int.Parse(parts[2]);
            if (num > items.Count)
            {
                var temp = new List<string>(items);
                items.Clear();
                return temp;
            }
            value = items.GetRange(0, num);
            items.RemoveRange(0, num);
        }

        return value;
    }

    public Task<string> BLPOP(string key)
    {
        var tcs = new TaskCompletionSource<string>();
        var queue = _waiters.GetOrAdd(key, _ => new Queue<TaskCompletionSource<string>>());
        lock (queue) { queue.Enqueue(tcs); }
        return tcs.Task;
    }

    public bool TryNotifyWaiter(string key, string value)
    {
        if (!_waiters.TryGetValue(key, out var queue)) return false;
        lock (queue)
        {
            if (queue.Count == 0) return false;
            queue.Dequeue().SetResult(value);
            return true;
        }
    }

    private T GetOrCreate<T>(string key) where T : RedisValue, new()
    {
        return (T)_store.GetOrAdd(key, _ => new T());
    }

    public string? TYPE(string key)
    {
        if (!_store.TryGetValue(key, out var value)) return "none";

        return value switch
        {
            RedisString => "string",
            RedisStream => "stream",
            RedisList => "list",
            _ => "unknown"
        };
    }


    public (bool Success, string Value) XADD(string key, string id, Dictionary<string, string> fields)
    {
        var stream = GetOrCreate<RedisStream>(key);

        if (id == "*")
            id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-*";

        var splitId = id.Split('-');
        var (timeInMs, sequence) = ResolveXADDId(splitId, stream);

        if (timeInMs == 0 && sequence == 0)
            return (false, "The ID specified in XADD must be greater than 0-0");

        if (stream.Entries.Count > 0)
        {
            var lastId = stream.Entries.Last().Id.Split('-');
            var lastTimeInMs = long.Parse(lastId[0]);
            var lastSequence = int.Parse(lastId[1]);

            if (timeInMs < lastTimeInMs || (timeInMs == lastTimeInMs && sequence <= lastSequence))
                return (false, "The ID specified in XADD is equal or smaller than the target stream top item");
        }

        var resolvedId = $"{timeInMs}-{sequence}";
        stream.Entries.Add((resolvedId, fields));
        return (true, resolvedId);
    }

    private (long timeInMs, int sequence) ResolveXADDId(string[] splitId, RedisStream stream)
    {
        var timeInMs = long.Parse(splitId[0]);
        int sequence;

        if (splitId[1] == "*")
        {
            if (stream.Entries.Count > 0)
            {
                var lastId = stream.Entries.Last().Id.Split('-');
                var lastTime = long.Parse(lastId[0]);
                var lastSeq = int.Parse(lastId[1]);
                sequence = timeInMs == lastTime ? lastSeq + 1 : 0;
            }
            else
            {
                sequence = timeInMs == 0 ? 1 : 0;
            }
        }
        else
        {
            sequence = int.Parse(splitId[1]);
        }

        return (timeInMs, sequence);
    }

    public List<(string Id, Dictionary<string, string> Fields)> XRange(string key, string startId, string endId)
    {
        if (!_store.TryGetValue(key, out var entry) || entry is not RedisStream stream)
            return new();

        var result = new List<(string Id, Dictionary<string, string> Fields)>();

        foreach (var item in stream.Entries)
        {
            if (IsIdInRange(item.Id, startId, endId))
                result.Add(item);
        }

        return result;
    }
    private bool IsIdInRange(string id, string startId, string endId)
    {
        long startTime = 0;
        var startSeq = 0;

        if (startId != "-")
        {
            var startParts = startId.Split('-');
            startTime = long.Parse(startParts[0]);
            startSeq = int.Parse(startParts[1]);
        }

        var idParts = id.Split('-');
        var idSeq = int.Parse(idParts[1]);
        var idTime = long.Parse(idParts[0]);

        var endParts = endId.Split('-');
        var endTime = long.Parse(endParts[0]);
        var endSeq = int.Parse(endParts[1]);

        return (idTime > startTime || (idTime == startTime && idSeq >= startSeq)) &&
               (idTime < endTime || (idTime == endTime && idSeq <= endSeq));
    }
}


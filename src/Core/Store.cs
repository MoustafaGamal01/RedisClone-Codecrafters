namespace codecrafters_redis.src.Core;

using codecrafters_redis.src.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

public class Store
{
    private readonly ConcurrentDictionary<string, RedisValue> _store = new();
    private readonly ConcurrentDictionary<string, Queue<TaskCompletionSource<string>>> _waiters = new();
    private readonly ConcurrentDictionary<string, List<TaskCompletionSource<bool>>> _streamWaiters = new();
    private readonly object _lock = new();
    public static bool multiState = false;

    public void Set(string key, string value, DateTime? expiresAt = null)
    {
        _store[key] = new RedisString { type = value, ExpiresAt = expiresAt };
    }

    public string? Get(string key)
    {
        if (!_store.TryGetValue(key, out var entry)) return null;

        if (entry.IsExpired)
        {
            _store.TryRemove(key, out _);
            return null;
        }

        return (entry as RedisString)?.type;
    }

    public int RPUSH(List<string> parts)
    {
        var key = parts[1];
        var list = GetOrCreate<RedisList>(key);

        for (int i = 2; i < parts.Count; i++)
            list.Items.Add(parts[i]);

        // wake BLPOP
        TryNotifyWaiter(key, list.Items.First());

        return list.Items.Count;
    }

    public int LPUSH(List<string> parts)
    {
        var key = parts[1];
        var list = GetOrCreate<RedisList>(key);

        for (int i = 2; i < parts.Count; i++)
            list.Items.Insert(0, parts[i]);

        TryNotifyWaiter(key, list.Items.First());

        return list.Items.Count;
    }

    public List<string> LRANGE(List<string> parts, int start, int stop)
    {
        var key = parts[1];

        if (!_store.TryGetValue(key, out var entry) || entry is not RedisList list)
            return new();

        var items = list.Items;
        var count = items.Count;

        if (start < 0) start = count + start;
        if (stop < 0) stop = count + stop;

        start = Math.Max(0, start);
        stop = Math.Min(stop, count - 1);

        if (start > stop || start >= count)
            return new();

        return items.GetRange(start, stop - start + 1);
    }

    public int LLEN(List<string> parts)
    {
        var key = parts[1];

        if (!_store.TryGetValue(key, out var entry) || entry is not RedisList list)
            return 0;

        return list.Items.Count;
    }

    public List<string>? LPOP(List<string> parts)
    {
        var key = parts[1];

        if (!_store.TryGetValue(key, out var entry) || entry is not RedisList list)
            return null;

        if (list.Items.Count == 0) return null;

        int count = parts.Count == 2 ? 1 : int.Parse(parts[2]);

        count = Math.Min(count, list.Items.Count);

        var result = list.Items.GetRange(0, count);
        list.Items.RemoveRange(0, count);

        return result;
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

            queue.Dequeue().TrySetResult(value);
            return true;
        }
    }

    public (bool Success, string Value) XADD(string key, string id, Dictionary<string, string> fields)
    {
        var stream = GetOrCreate<RedisStream>(key);

        if (id == "*")
            id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-*";

        var (time, seq) = ResolveXADDId(id.Split('-'), stream);

        if (time == 0 && seq == 0)
            return (false, "The ID specified in XADD must be greater than 0-0");

        if (stream.Entries.Count > 0)
        {
            var (lastTime, lastSeq) = ParseId(stream.Entries.Last().Id);

            if (time < lastTime || (time == lastTime && seq <= lastSeq))
                return (false, "The ID specified in XADD is equal or smaller than the target stream top item");
        }

        var resolved = $"{time}-{seq}";
        stream.Entries.Add((resolved, fields));

        NotifyStreamWaiters(key);

        return (true, resolved);
    }

    public List<(string Id, Dictionary<string, string> Fields)> XREAD(string key, string lastId)
    {
        if (!_store.TryGetValue(key, out var entry) || entry is not RedisStream stream)
            return new();

        var result = new List<(string, Dictionary<string, string>)>();

        foreach (var item in stream.Entries)
        {
            if (IsGreater(item.Id, lastId, stream))
                result.Add(item);
        }

        return result;
    }

    public async Task<List<(string StreamKey, List<(string Id, Dictionary<string, string> Fields)> Entries)>>
        XRead(List<string> keys, List<string> ids, int blockMs)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == "$")
            {
                if (_store.TryGetValue(keys[i], out var entry) &&
                    entry is RedisStream stream &&
                    stream.Entries.Count > 0)
                {
                    ids[i] = stream.Entries.Last().Id;
                }
                else
                {
                    ids[i] = "0-0";
                }
            }
        }

        var result = ReadAvailable(keys, ids);

        if (result.Count > 0)
            return result;

        // no blocking → return empty
        if (blockMs < 0)
            return new();

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            foreach (var key in keys)
            {
                var waiters = _streamWaiters.GetOrAdd(key, _ => new List<TaskCompletionSource<bool>>());
                waiters.Add(tcs);
            }
        }

        Task completed;

        if (blockMs == 0)
        {
            // BLOCK FOREVER
            completed = await Task.WhenAny(tcs.Task);
        }
        else
        {
            var delay = Task.Delay(blockMs);
            completed = await Task.WhenAny(tcs.Task, delay);

            if (completed == delay)
            {
                RemoveWaiter(tcs, keys);
                return new();
            }
        }

        RemoveWaiter(tcs, keys);

        return ReadAvailable(keys, ids);
    }

    private void RemoveWaiter(TaskCompletionSource<bool> tcs, List<string> keys)
    {
        lock (_lock)
        {
            foreach (var key in keys)
            {
                if (_streamWaiters.TryGetValue(key, out var list))
                {
                    list.Remove(tcs);
                }
            }
        }
    }

    private List<(string StreamKey, List<(string Id, Dictionary<string, string> Fields)> Entries)>
    ReadAvailable(List<string> keys, List<string> ids)
    {
        var result = new List<(string, List<(string, Dictionary<string, string>)>)>();

        for (int i = 0; i < keys.Count; i++)
        {
            var entries = XREAD(keys[i], ids[i]);
            if (entries.Count > 0)
                result.Add((keys[i], entries));
        }

        return result;
    }

    private void NotifyStreamWaiters(string key)
    {
        if (!_streamWaiters.TryGetValue(key, out var list)) return;

        lock (list)
        {
            foreach (var t in list)
                t.TrySetResult(true);

            list.Clear();
        }
    }

    private (long, int) ResolveXADDId(string[] parts, RedisStream stream)
    {
        long time = long.Parse(parts[0]);
        int seq;

        if (parts[1] == "*")
        {
            if (stream.Entries.Count == 0)
                seq = time == 0 ? 1 : 0;
            else
            {
                var (lastTime, lastSeq) = ParseId(stream.Entries.Last().Id);
                seq = time == lastTime ? lastSeq + 1 : 0;
            }
        }
        else
        {
            seq = int.Parse(parts[1]);
        }

        return (time, seq);
    }

    private (long time, int seq) ParseId(string id)
    {
        var parts = id.Split('-');
        return (long.Parse(parts[0]), int.Parse(parts[1]));
    }

    private bool IsGreater(string id, string lastId, RedisStream stream)
    {
        if (lastId == "$")
        {
            if (stream.Entries.Count == 0) return false;
            lastId = stream.Entries.Last().Id;
        }

        var (t1, s1) = ParseId(id);
        var (t2, s2) = ParseId(lastId);

        return t1 > t2 || (t1 == t2 && s1 > s2);
    }

    private T GetOrCreate<T>(string key) where T : RedisValue, new()
    {
        return (T)_store.GetOrAdd(key, _ => new T());
    }

    public List<(string Id, Dictionary<string, string> Fields)> XRange(string key, string startId, string endId) 
    {
        if (!_store.TryGetValue(key, out var entry) || entry is not RedisStream stream)
            return new(); 
        var result = new List<(string Id, Dictionary<string, string> Fields)>(); 
        
        foreach (var item in stream.Entries)
        {
            if (IsIdInRange(item.Id, startId, endId)) result.Add(item); 
        } 
        return result; 
    }
    
    private bool IsIdInRange(string id, string startId, string endId)
    {
        long startTime = 0, endTime = long.MaxValue; int startSeq = 0, endSeq = int.MaxValue; 
        var idParts = id.Split('-'); 
        var idSeq = int.Parse(idParts[1]); 
        var idTime = long.Parse(idParts[0]); 
        if (startId != "-") 
        {
            var startParts = startId.Split('-'); 
            startTime = long.Parse(startParts[0]);
            startSeq = int.Parse(startParts[1]); 
        } 
        if (endId != "+") 
        {
            var endParts = endId.Split('-');
            endTime = long.Parse(endParts[0]);
            endSeq = int.Parse(endParts[1]);
        } 
        return (idTime > startTime 
            || (idTime == startTime && idSeq >= startSeq)) 
            && (idTime < endTime || (idTime == endTime && idSeq <= endSeq)); 
    }

    public string TYPE(string key)
    {
        if (!_store.TryGetValue(key, out var value)) return "none";

        return value switch
        {
            RedisString => "string",
            RedisList => "list",
            RedisStream => "stream",
            _ => "unknown"
        };
    }

    public (bool, int) INCR(string key)
    {
        var entry = GetOrCreate<RedisString>(key);

        // If the key is new, initialize it to "1"
        if (entry.type == null)
        {
            entry.type = "1";
            return (true, 1);
        }


        int number = 0;
        var success = int.TryParse(entry.type, out number);
        // If the current value is a valid integer
        if (success)
        {
            number++;
            entry.type = number.ToString();
        }

        return (success, number);
    }

    public string MULTI()
    {
        multiState = true;  
        return "OK";
    }

    public string EXEC()
    {
        if(multiState == false) return "EXEC without MULTI";
        return "OK";
    }

}
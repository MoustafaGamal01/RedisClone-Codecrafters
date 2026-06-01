using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using codecrafters_redis.src.Redis;

namespace codecrafters_redis.src.Core
{
    public class Store
    {
        private readonly DatabaseStorage _db;
        private readonly GeoService _geo;
        private readonly PubSubBroker _pubsub;
        private readonly object _lock = new();

        public Store()
        {
            _db = new DatabaseStorage();
            _geo = new GeoService(_db);
            _pubsub = new PubSubBroker();
        }
        public DatabaseStorage Db => _db;

        public void IncrementVersion(string key) => _db.IncrementVersion(key);
        public long GetVersion(string key) => _db.GetVersion(key);
        public bool Set(string key, string value, DateTime? expiresAt = null) => _db.Set(key, value, expiresAt);
        public string? Get(string key) => _db.Get(key);

        public int RPush(List<string> parts)
        {
            var key = parts[1];
            _db.IncrementVersion(key);
            var list = _db.GetOrCreate<RedisList>(key);

            for (int i = 2; i < parts.Count; i++)
                list.Items.Add(parts[i]);

            int returnCount = list.Items.Count;

            // wake BLPOP
            while (list.Items.Count > 0 && TryNotifyWaiter(key, out var tcs))
            {
                var val = list.Items[0];
                list.Items.RemoveAt(0);
                tcs.TrySetResult(val);
            }

            return returnCount;
        }

        public int LPush(List<string> parts)
        {
            var key = parts[1];
            _db.IncrementVersion(key);
            var list = _db.GetOrCreate<RedisList>(key);

            for (int i = 2; i < parts.Count; i++)
                list.Items.Insert(0, parts[i]);

            int returnCount = list.Items.Count;

            while (list.Items.Count > 0 && TryNotifyWaiter(key, out var tcs))
            {
                var val = list.Items[0];
                list.Items.RemoveAt(0);
                tcs.TrySetResult(val);
            }

            return returnCount;
        }

        public List<string> LRange(List<string> parts, int start, int stop)
        {
            var key = parts[1];

            if (!_db.RawStore.TryGetValue(key, out var entry) || entry is not RedisList list)
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

        public int LLen(List<string> parts)
        {
            var key = parts[1];

            if (!_db.RawStore.TryGetValue(key, out var entry) || entry is not RedisList list)
                return 0;

            return list.Items.Count;
        }

        public List<string>? LPop(List<string> parts)
        {
            var key = parts[1];

            if (!_db.RawStore.TryGetValue(key, out var entry) || entry is not RedisList list)
                return null;

            if (list.Items.Count == 0) return null;

            _db.IncrementVersion(key);

            int count = parts.Count == 2 ? 1 : int.Parse(parts[2]);

            count = Math.Min(count, list.Items.Count);

            var result = list.Items.GetRange(0, count);
            list.Items.RemoveRange(0, count);

            return result;
        }

        public Task<string> BLPop(string key, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var queue = _db.Waiters.GetOrAdd(key, _ => new Queue<TaskCompletionSource<string>>());

            lock (queue)
            {
                queue.Enqueue(tcs);
            }

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled());
            }

            return tcs.Task;
        }

        public bool TryNotifyWaiter(string key, out TaskCompletionSource<string>? tcs)
        {
            tcs = null;
            if (!_db.Waiters.TryGetValue(key, out var queue)) return false;

            lock (queue)
            {
                while (queue.Count > 0)
                {
                    var task = queue.Dequeue();
                    if (!task.Task.IsCompleted)
                    {
                        tcs = task;
                        return true;
                    }
                }
                return false;
            }
        }

        public (bool Success, string Value) XAdd(string key, string id, Dictionary<string, string> fields)
        {
            var stream = _db.GetOrCreate<RedisStream>(key);

            if (id == "*")
                id = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-*";

            var (time, seq) = ResolveXAddId(id.Split('-'), stream);

            if (time == 0 && seq == 0)
                return (false, "The ID specified in XADD must be greater than 0-0");

            if (stream.Entries.Count > 0)
            {
                var (lastTime, lastSeq) = ParseId(stream.Entries.Last().Id);

                if (time < lastTime || (time == lastTime && seq <= lastSeq))
                    return (false, "The ID specified in XADD is equal or smaller than the target stream top item");
            }

            var resolved = $"{time}-{seq}";
            _db.IncrementVersion(key);
            stream.Entries.Add((resolved, fields));

            NotifyStreamWaiters(key);

            return (true, resolved);
        }

        public List<(string Id, Dictionary<string, string> Fields)> XRead(string key, string lastId)
        {
            if (!_db.RawStore.TryGetValue(key, out var entry) || entry is not RedisStream stream)
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
                    if (_db.RawStore.TryGetValue(keys[i], out var entry) &&
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
                    var waiters = _db.StreamWaiters.GetOrAdd(key, _ => new List<TaskCompletionSource<bool>>());
                    waiters.Add(tcs);
                }
            }

            Task completed;

            if (blockMs == 0)
            {
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
                    if (_db.StreamWaiters.TryGetValue(key, out var list))
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
                var entries = XRead(keys[i], ids[i]);
                if (entries.Count > 0)
                    result.Add((keys[i], entries));
            }

            return result;
        }

        private void NotifyStreamWaiters(string key)
        {
            if (!_db.StreamWaiters.TryGetValue(key, out var list)) return;

            lock (list)
            {
                foreach (var t in list)
                    t.TrySetResult(true);

                list.Clear();
            }
        }

        private (long, int) ResolveXAddId(string[] parts, RedisStream stream)
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

        public List<(string Id, Dictionary<string, string> Fields)> XRange(string key, string startId, string endId) 
        {
            if (!_db.RawStore.TryGetValue(key, out var entry) || entry is not RedisStream stream)
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

        public string Type(string key)
        {
            if (!_db.RawStore.TryGetValue(key, out var value)) return "none";

            return value switch
            {
                RedisString => "string",
                RedisList => "list",
                RedisStream => "stream",
                _ => "unknown"
            };
        }

        public (bool, int) Incr(string key) => _db.Incr(key);
        public void SetConfig(string key, string value) => _db.SetConfig(key, value);
        public string? GetConfig(string key) => _db.GetConfig(key);
        public List<string> Keys(string key) => _db.Keys(key);

        public void LoadRdb()
        {
            var dir = GetConfig("dir");
            var dbfilename = GetConfig("dbfilename");

            if (dir != null && dbfilename != null)
            {
                var path = System.IO.Path.Combine(dir, dbfilename);
                var loadedStore = RdbLoader.Load(path);
                foreach (var kvp in loadedStore)
                {
                    _db.RawStore[kvp.Key] = kvp.Value;
                }
            }
        }

        // Pub/Sub Delegations
        public void Subscribe(string channel, NetworkStream stream) => _pubsub.Subscribe(channel, stream);
        public int Unsubscribe(string channel, NetworkStream stream) => _pubsub.Unsubscribe(channel, stream);
        public List<NetworkStream> Publish(string channel) => _pubsub.Publish(channel);

        // Sorted Set & Geo Delegations
        public bool ZAdd(List<string> parts) => _geo.ZAdd(parts);
        public int ZRank(string key, string member) => _geo.ZRank(key, member);
        public List<string> ZRange(string key, int start, int end) => _geo.ZRange(key, start, end);
        public int ZCard(string key) => _geo.ZCard(key);
        public double ZScore(string key, string member) => _geo.ZScore(key, member);
        public int ZRem(string key, string member) => _geo.ZRem(key, member);

        public int GeoAdd(string key, double longitude, double latitude, string place) => _geo.GeoAdd(key, longitude, latitude, place);
        public List<(double, double)?> GeoPos(string key, List<string> places) => _geo.GeoPos(key, places);
        public double GeoDist(string key, string place1, string place2) => _geo.GeoDist(key, place1, place2);
        public List<string> GeoSearch(string key, double lon, double lat, double distance) => _geo.GeoSearch(key, lon, lat, distance);

        // User Passwords Delegations
        public List<string> GetUserPasswords(string username) => _db.GetUserPasswords(username);
        public void AddUserPassword(string username, string passwordHash) => _db.AddUserPassword(username, passwordHash);
        public void ClearUserPasswords(string username) => _db.ClearUserPasswords(username);
        public bool IsValidPassword(string username, string passwordHash) => _db.IsValidPassword(username, passwordHash);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using codecrafters_redis.src.Common.Geo;

namespace codecrafters_redis.src.Core
{
    public class GeoService
    {
        private readonly DatabaseStorage _db;

        public GeoService(DatabaseStorage db)
        {
            _db = db;
        }

        public bool ZAdd(List<string> parts)
        {
            var key = parts[1];
            var score = double.Parse(parts[2]);
            var member = parts[3];

            var set = _db.ZAddStore.GetOrAdd(key, _ => new SortedSet<(double, string)>(RedisScoreComparer.Instance));

            lock (set)
            {
                var existing = set.FirstOrDefault(x => x.value == member);
                if (existing.value != null)
                {
                    set.Remove(existing);
                    set.Add((score, member));
                    return false; 
                }

                set.Add((score, member));
                return true;
            }
        }

        public int ZRank(string key, string member)
        {
            if (!_db.ZAddStore.TryGetValue(key, out var set)) return -1;

            lock (set)
            {
                int rank = 0;
                foreach (var entry in set) 
                {
                    if (entry.value == member) return rank;
                    rank++;
                }
                return -1;
            }
        }

        public List<string> ZRange(string key, int start, int end)
        {
            if (!_db.ZAddStore.TryGetValue(key, out var set)) return new();

            lock (set)
            {
                var list = set.ToList(); 
                int max = list.Count - 1;

                if (start < 0) start = max + start + 1;
                if (end < 0) end = max + end + 1;

                start = Math.Max(0, start);
                end = Math.Min(end, max);

                if (start > end) return new();

                return list.Skip(start).Take(end - start + 1).Select(x => x.value).ToList();
            }
        }

        public int ZCard(string key)
        {
            if (!_db.ZAddStore.TryGetValue(key, out var set)) return 0;

            lock (set) { return set.Count; }
        }

        public double ZScore(string key, string member)
        {
            if(_db.ZAddStore.TryGetValue(key, out var set))
            {
                lock (set)
                {
                    var existingMember = set.FirstOrDefault(x => x.value == member);

                    if (existingMember.value != null)
                    {
                        return existingMember.score;
                    }

                    return -1;
                }
            }
            return -1;
        }

        public int ZRem(string key, string member)
        {
            if (!_db.ZAddStore.TryGetValue(key, out var set)) return 0;
            lock (set)
            {
                var existingMember = set.FirstOrDefault(x => x.value == member);
                if (existingMember.value != null)
                {
                    set.Remove(existingMember);
                    return 1;
                }
                return 0;
            }
        }

        public int GeoAdd(string key, double longitude, double latitude, string place)
        {
            var score = RedisGeohashEncoder.Encode(latitude, longitude); 
            ZAdd(new List<string> { "ZADD", key, score.ToString(), place });
            return 1;
        }

        public List<(double, double)?> GeoPos(string key, List<string> places)
        {
            var result = new List<(double, double)?>();

            foreach (var place in places)
            {
                if (!_db.ZAddStore.TryGetValue(key, out var set))
                {
                    result.Add(null);
                    continue;
                }

                lock (set)
                {
                    var entry = set.FirstOrDefault(x => x.value == place);
                    if (entry.value != null)
                    {
                        var decode = RedisGeohashDecoder.Decode((long)entry.score);
                        result.Add(decode);
                    }
                    else
                        result.Add(null);
                }
            }
            return result;
        }

        public double GeoDist(string key, string place1, string place2)
        {
            if (!_db.ZAddStore.TryGetValue(key, out var set)) return -1;
            (double latitude, double longitude)? pos1 = null;
            (double latitude, double longitude)? pos2 = null;

            lock (set)
            {
                var entry1 = set.FirstOrDefault(x => x.value == place1);
                if (entry1.value != null)
                    pos1 = RedisGeohashDecoder.Decode((long)entry1.score);
                var entry2 = set.FirstOrDefault(x => x.value == place2);
                if (entry2.value != null)
                    pos2 = RedisGeohashDecoder.Decode((long)entry2.score);
            }
            if (pos1 == null || pos2 == null) return -1;
            return RedisHaversine.calculate(pos1.Value.latitude, pos1.Value.longitude, pos2.Value.latitude, pos2.Value.longitude);
        }

        public List<string> GeoSearch(string key, double lon, double lat, double distance)
        {
            List<string> result = new List<string>();
            
            _db.ZAddStore.Where(kvp => kvp.Key == key).ToList().ForEach(kvp =>
            {
                lock (kvp.Value)
                {
                    foreach (var entry in kvp.Value)
                    {
                        var pos = RedisGeohashDecoder.Decode((long)entry.score);
                        var dist = RedisHaversine.calculate(lat, lon, pos.latitude, pos.longitude);
                        if (dist <= distance)
                            result.Add(entry.value);
                    }
                }
            });

            return result;
        }
    }
}

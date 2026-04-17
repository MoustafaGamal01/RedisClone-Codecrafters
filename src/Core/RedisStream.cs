using codecrafters_redis.src.Redis;

namespace codecrafters_redis.src.Core;

internal class RedisStream : RedisValue
{
    public List<(string Id, Dictionary<string, string> Fields)> Entries { get; } = new();
}

namespace codecrafters_redis.src.Redis;

internal class RedisStream : RedisValue
{
    public List<(string Id, Dictionary<string, string> Fields)> Entries { get; } = new();
}

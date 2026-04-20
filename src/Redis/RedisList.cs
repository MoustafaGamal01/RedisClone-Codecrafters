namespace codecrafters_redis.src.Redis;

public class RedisList : RedisValue
{
    public List<string> Items { get; set; } = new();
}

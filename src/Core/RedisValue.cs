namespace codecrafters_redis.src.Redis;

public abstract class RedisValue
{
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}

namespace codecrafters_redis.src.Core;

public class ClientContext
{
    public bool IsInTransaction { get; set; } = false;
    public Dictionary<string, long> WatchedKeys { get; } = new();
    public Queue<List<string>> CommandQueue { get; } = new();
}
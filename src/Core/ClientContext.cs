namespace codecrafters_redis.src.Core;

public class ClientContext
{
    public bool IsInTransaction { get; set; } = false;
    public Queue<List<string>> CommandQueue { get; } = new();
}
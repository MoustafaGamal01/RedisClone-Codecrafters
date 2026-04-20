namespace codecrafters_redis.src.Client;

public class ClientContext
{
    public bool IsInTransaction { get; set; } = false;
    public Dictionary<string, long> WatchedKeys { get; } = new();
    public Dictionary<string, string> ClientRole { get; } = new();

    public int slaveCount = 0;  
    public Queue<List<string>> CommandQueue { get; } = new();
}
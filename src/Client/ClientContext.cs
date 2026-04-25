namespace codecrafters_redis.src.Client;

public class ClientContext
{
    public IReplicationRole Replication { get; set; }

    public bool IsInTransaction { get; set; } = false;
    public Dictionary<string, long> WatchedKeys { get; } = new();
    public Dictionary<string, string> ClientRole { get; } = new();

    public int slaveCount = 0;
    public bool SuppressResponses { get; set; } = false;
    public Queue<List<string>> CommandQueue { get; } = new();
}
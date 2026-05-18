namespace codecrafters_redis.src.Client;

public class ClientContext
{
    public IReplicationRole Replication { get; set; }
    public bool IsInTransaction { get; set; } = false;
    public Dictionary<string, long> WatchedKeys { get; } = new();
    public Dictionary<string, string> ClientRole { get; } = new();  
    public List<string> SubscribedChannels { get; } = new();
    public long ReplicationOffset { get; set; } = 0;
    public bool SuppressResponses { get; set; } = false;

    public List<string> passwords { get; } =  new();
    public Queue<List<string>> CommandQueue { get; } = new();
}
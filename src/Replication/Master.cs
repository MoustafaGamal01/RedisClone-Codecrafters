namespace codecrafters_redis.src.Replication;

internal class Master : IReplicationRole
{
    public string Role => "master";
    public readonly string ReplId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
    public long ReplOffset = 0;
    public int ConnectedReplicaCount => _replicas.Count;  

    private readonly List<NetworkStream> _replicas = new();
    private readonly Dictionary<NetworkStream, long> _replicaOffsets = new();


    public Task StartAsync(int port) => Task.CompletedTask;
    public void RegisterReplica(NetworkStream stream)
    {
        _replicas.Add(stream);
        _replicaOffsets[stream] = 0;
    }

    public void UpdateReplicaOffset(NetworkStream stream, long offset)
    {
        _replicaOffsets[stream] = offset;
    }

    public int GetSyncedReplicaCount()
    {
        return _replicaOffsets.Values.Count(v => v >= ReplOffset);
    }

    public async Task SendGetAck()
    {
        var bytes = Encoding.UTF8.GetBytes("*3\r\n$8\r\nREPLCONF\r\n$6\r\nGETACK\r\n$1\r\n*\r\n");
        foreach (var replica in _replicas)
        {
            await replica.WriteAsync(bytes);
        }
    }


    public async Task PropagateCommand(string command, List<string> parts)
    {
        var sb = new StringBuilder();
        sb.Append($"*{parts.Count + 1}\r\n");
        sb.Append($"${command.Length}\r\n{command}\r\n");
        foreach (var part in parts)
            sb.Append($"${part.Length}\r\n{part}\r\n");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        await SendToReplicas(bytes);

    }
    public async Task SendToReplicas(byte[] bytes)
    {
        foreach (var replica in _replicas)
        {
            await replica.WriteAsync(bytes);
        }

        ReplOffset += bytes.Length;
    }

}
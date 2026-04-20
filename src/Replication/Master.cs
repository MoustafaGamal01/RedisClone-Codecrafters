namespace codecrafters_redis.src.Replication;

internal class Master : IReplicationRole
{
    public string Role => "master";
    public readonly string ReplId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
    public int ReplOffset = 0;

    private readonly List<NetworkStream> _replicas = new();

    public Task StartAsync(int port) => Task.CompletedTask;
    public void RegisterReplica(NetworkStream stream)
    {
        _replicas.Add(stream);
    }

    public async Task PropagateCommand(string command, List<string> parts)
    {
        var sb = new StringBuilder();
        sb.Append($"*{parts.Count + 1}\r\n");
        sb.Append($"${command.Length}\r\n{command}\r\n");
        foreach (var part in parts)
            sb.Append($"${part.Length}\r\n{part}\r\n");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        foreach (var replica in _replicas)
        {
            await replica.WriteAsync(bytes);
        }
    }

}
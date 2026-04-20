namespace codecrafters_redis.src.Replication;

internal class Master : IReplicationRole
{
    public string Role => "master";
    private readonly List<NetworkStream> _replicas = new();
    private readonly string _replId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";

    public Task StartAsync(int port)
    {
        return Task.CompletedTask;
    }

    public async Task OnReplicaConnected(NetworkStream stream)
    {
        _replicas.Add(stream);
    }

    public async Task PropagateCommand(byte[] respCommand)
    {
        foreach (var replica in _replicas)
        {
            await replica.WriteAsync(respCommand);
        }
    }
}
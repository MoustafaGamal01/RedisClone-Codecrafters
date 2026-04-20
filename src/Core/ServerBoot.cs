using System.Net;

namespace codecrafters_redis.src.Core;

internal class ServerBoot
{
    private readonly int _port;
    private readonly IReplicationRole _replication;
    private readonly string[] _args;

    public ServerBoot(string[] args)
    {


        _args = args;
        string? replicaOf = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port") _port = int.Parse(args[i + 1]);
            if (args[i] == "--replicaof") replicaOf = args[i + 1];
        }

        if (_port == 0) _port = 6379;

        if (replicaOf != null)
        {
            var parts = replicaOf.Split(' ');
            _replication = new Replica(parts[0], int.Parse(parts[1]));
        }
        else
        {
            _replication = new Master();
        }
    }

    public async Task RunAsync()
    {
        await _replication.StartAsync(_port);
        await StartListening();
    }

    private async Task StartListening()
    {
        var sharedContext = new ClientContext();
        sharedContext.Replication = _replication;
        sharedContext.ClientRole["role"] = _replication.Role;


        var store = new Store();
        var dispatcher = new CommandHandler(store);
        var clientHandler = new ClientHandler(dispatcher, _args, sharedContext);
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"[{_replication.Role}] Listening on port {_port}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => clientHandler.HandleAsync(client));
        }
    }
}
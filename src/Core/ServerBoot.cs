using codecrafters_redis.src.Client;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Replication;
using System.Net;
using System.Net.Sockets;

namespace codecrafters_redis.src.Core;

internal class ServerBoot
{
    private readonly int _port;
    private readonly IReplicationRole _replication;
    private readonly CommandHandler _dispatcher;
    private readonly string[] _args;
    public ServerBoot(string[] args)
    {
        _args = args;
        string? replicaOf = null;
        string? dir = null;
        string? dbfilename = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port") _port = int.Parse(args[i + 1]);
            if (args[i] == "--replicaof") replicaOf = args[i + 1];
            if (args[i] == "--dir") dir = args[i + 1];
            if (args[i] == "--dbfilename") dbfilename = args[i + 1];
        }

        if (_port == 0) _port = 6379;

        var store = new Store();
        if (dir != null) store.SetConfig("dir", dir);
        if (dbfilename != null) store.SetConfig("dbfilename", dbfilename);
        
        store.LoadRdb();

        _dispatcher = new CommandHandler(store);

        if (replicaOf != null)
        {
            var parts = replicaOf.Split(' ');
            _replication = new Replica(parts[0], int.Parse(parts[1]), _dispatcher);
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

        var clientHandler = new ClientHandler(_dispatcher, _args, sharedContext);
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
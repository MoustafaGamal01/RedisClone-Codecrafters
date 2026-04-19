using codecrafters_redis.src.Client;
using codecrafters_redis.src.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Core;

internal class ServerBoot
{
    private readonly int _port;
    private readonly string? _replicaOf;
    private readonly string[] _args;

    public ServerBoot(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port") _port = int.Parse(args[i + 1]);
            if (args[i] == "--replicaof") _replicaOf = args[i + 1];
        }
        if (_port == 0) _port = 6379;
        _args = args;
    }

    public async Task RunAsync()
    {
        await PerformReplicaHandshakeIfNeeded();
        await StartListening();
    }

    private async Task PerformReplicaHandshakeIfNeeded()
    {
        if (_replicaOf == null) return;

        var parts = _replicaOf.Split(' ');
        var masterClient = new TcpClient();
        await masterClient.ConnectAsync(parts[0], int.Parse(parts[1]));
        
        var stream = masterClient.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n"));

        var buffer = new byte[1024];
        var bytes = await stream.ReadAsync(buffer);
        Console.WriteLine($"Master: {Encoding.UTF8.GetString(buffer, 0, bytes).Trim()}");

        // the second step of the replication handshake.
        var replconfCommand1 = $"*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n$4\r\n{_port}\r\n";
        var replconfCommand2 = $"*3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";

        var replconfBytes1 = Encoding.UTF8.GetBytes(replconfCommand1);
        var replconfBytes2 = Encoding.UTF8.GetBytes(replconfCommand2);

        // Send the REPLCONF commands to the master
        await stream.WriteAsync(replconfBytes1);
        await stream.WriteAsync(replconfBytes2);

        bytes = await stream.ReadAsync(buffer);

        var psyncCommand = $"*3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";

        await stream.WriteAsync(Encoding.UTF8.GetBytes(psyncCommand));

        bytes = await stream.ReadAsync(buffer);
    }

    private async Task StartListening()
    {
        var store = new Store();
        var dispatcher = new CommandHandler(store);
        var clientHandler = new ClientHandler(dispatcher, _args);
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"Listening on port {_port}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => clientHandler.HandleAsync(client));
        }
    }
}


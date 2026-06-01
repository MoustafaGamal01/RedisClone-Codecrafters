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
    private readonly Store _store;
    public ServerBoot(string[] args)
    {
        _args = args;
        _store = new Store();
        string? replicaOf = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port") _port = int.Parse(args[i + 1]);
            if (args[i] == "--replicaof") replicaOf = args[i + 1];
            if (args[i] == "--dir")
            {
                _store.SetConfig("dir", args[i + 1]);
            }
            if (args[i] == "--appendonly")
            {
                _store.SetConfig("appendonly", args[i + 1]);
            }
            if (args[i] == "--appenddirname") 
            { 
                _store.SetConfig("appenddirname", args[i + 1]);
            }
            if (args[i] == "--appendfilename")
            {
                _store.SetConfig("appendfilename", args[i + 1]);
            }
            if (args[i] == "--appendfsync")
            {
                _store.SetConfig("appendfsync", args[i + 1]);
            }
            if (args[i] == "--dbfilename")
            {
                _store.SetConfig("dbfilename", args[i + 1]);
            }
        }

        if (_port == 0) _port = 6379;

        _store.LoadRdb();

        if (_store.GetConfig("appendonly") == "yes")
        {
            var dir = _store.GetConfig("dir");
            var appenddirname = _store.GetConfig("appenddirname");
            var appendfilename = _store.GetConfig("appendfilename");
            if (dir != null && appenddirname != null && appendfilename != null)
            {
                var aofDir = System.IO.Path.Combine(dir, appenddirname);
                System.IO.Directory.CreateDirectory(aofDir);

                var manifestFilePath = System.IO.Path.Combine(aofDir, $"{appendfilename}.manifest");
                string activeAofFile = $"{appendfilename}.1.incr.aof";

                if (System.IO.File.Exists(manifestFilePath))
                {
                    var lines = System.IO.File.ReadAllLines(manifestFilePath);
                    foreach (var line in lines)
                    {
                        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length >= 6 && tokens[0] == "file" && tokens[2] == "seq" && tokens[4] == "type" && tokens[5] == "i")
                        {
                            activeAofFile = tokens[1];
                            break;
                        }
                    }
                }
                else
                {
                    var aofFilePath = System.IO.Path.Combine(aofDir, activeAofFile);
                    if (!System.IO.File.Exists(aofFilePath))
                    {
                        System.IO.File.Create(aofFilePath).Dispose();
                    }

                    var manifestContent = $"file {activeAofFile} seq 1 type i\n";
                    System.IO.File.WriteAllText(manifestFilePath, manifestContent);
                }

                var finalAofFilePath = System.IO.Path.Combine(aofDir, activeAofFile);
                _store.SetConfig("active_aof_path", finalAofFilePath);
            }
        }

        _dispatcher = new CommandHandler(_store);

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

        var clientHandler = new ClientHandler(_dispatcher, _args, sharedContext, _store);
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => clientHandler.HandleAsync(client));
        }
    }
}
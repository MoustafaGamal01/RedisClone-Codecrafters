namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Client;
using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

public class CommandHandler
{
    private readonly Dictionary<string, ICommandHandler> _handlers;
    private readonly Store _store;

    private static readonly HashSet<string> WriteCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "SET", "RPUSH", "LPUSH", "LPOP", "XADD", "INCR", "ZADD", "ZREM", "GEOADD", "BLPOP"
    };

    private static readonly object _aofLock = new object();

    public CommandHandler(Store store)
    {
        _store = store;
        var commands = new List<ICommandHandler>
        {
            new PingHandler(),
            new EchoHandler(),
            new SetHandler(store),
            new GetHandler(store),
            new RPushHandler(store),
            new LRangeHandler(store),
            new BLPopHandler(store),
            new LLenHandler(store),
            new LPopHandler(store),
            new LPushHandler(store),
            new TypeHandler(store),
            new XAddHandler(store),
            new XRangeHandler(store),
            new XReadHandler(store),
            new IncrHandler(store),
            new MultiHandler(),
            new ExecHandler(this, store),
            new DiscardHandler(),
            new WatchHandler(store),
            new UnwatchHandler(),
            new InfoHandler(),
            new ReplConfHandler(),
            new PsyncHandler(),
            new WaitHandler(),
            new ConfigHandler(store),
            new KeysHandler(store),
            new SubscribeHandler(store),
            new PublishHandler(store),
            new UnsubscribeHandler(store),
            new ZAddHandler(store),
            new ZRankHandler(store),    
            new ZRangeHandler(store),
            new ZCardHandler(store),
            new ZScoreHandler(store),
            new ZRemHandler(store),
            new GeoAddHandler(store),
            new GeoPosHandler(store), 
            new GeoDistHandler(store),
            new GeoSearchHandler(store),
            new WhoAmIHandler(),
            new GetUserHandler(store),
            new SetUserHandler(store),   
            new AuthHandler(store),
        };

        _handlers = commands.ToDictionary(c => c.CommandName.ToString());
    }

    public async Task Dispatch(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count == 0) { await RespWriter.WriteError(stream, "Empty command"); return; }

        if (context.AuthenticatedUser == null && !parts[0].Equals("AUTH", StringComparison.OrdinalIgnoreCase))
        {
            await RespWriter.WriteSimpleError(stream, "NOAUTH Authentication required.");
            return;
        }

        var command = parts[0].ToUpper();
        if (command == "ACL" && parts.Count > 1)
        {
            command = parts[1].ToUpper();
        }

        if (context.IsInTransaction && command != "EXEC" && command != "DISCARD" && command != "WATCH" && command != "MULTI")
        {
            context.CommandQueue.Enqueue(parts);
            await RespWriter.WriteSimpleString(stream, "QUEUED");
            return;
        }

        if (context.SubscribedChannels.Count > 0)
        {
            if(command != "SUBSCRIBE" && command != "UNSUBSCRIBE" && command != "PING"
                && command != "QUIT" && command != "RESET")
            {
                await RespWriter.WriteError(stream, $"can't execute '{command.ToLower()}'");
                return;
            }
        }

        if (_handlers.TryGetValue(command, out var handler))
        {
            if (_store.GetConfig("appendonly") == "yes" && WriteCommands.Contains(command))
            {
                var aofPath = _store.GetConfig("active_aof_path");
                if (!string.IsNullOrEmpty(aofPath))
                {
                    var respCommand = ToResp(parts);
                    WriteToAof(aofPath, respCommand);
                }
            }

            await handler.Handle(stream, parts, context);
        }
        else
        {
            await RespWriter.WriteError(stream, $"Unknown command '{command}'");
        }
    }

    private static string ToResp(List<string> parts)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"*{parts.Count}\r\n");
        foreach (var part in parts)
        {
            sb.Append($"${part.Length}\r\n{part}\r\n");
        }
        return sb.ToString();
    }

    private void WriteToAof(string path, string data)
    {
        lock (_aofLock)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
            {
                fs.Write(bytes, 0, bytes.Length);
                var appendfsync = _store.GetConfig("appendfsync");
                if (appendfsync == "always")
                {
                    fs.Flush(true); // forces fsync to disk
                }
                else
                {
                    fs.Flush(false);
                }
            }
        }
    }
}
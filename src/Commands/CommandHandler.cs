namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

public class CommandHandler
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public CommandHandler(Store store)
    {
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
            new XaddHandler(store),
            new XRangeHandler(store),
            new XReadHandler(store),
            new IncrHandler(store),
            new MultiHandler(),
            new ExecHandler(this),
            new DiscardHandler(),
            new WatchHandler(),
        };

        _handlers = commands.ToDictionary(c => c.CommandName.ToString());
    }

    public async Task Dispatch(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count == 0) { await RespWriter.WriteError(stream, "Empty command"); return; }

        var command = parts[0].ToUpper();

        if (context.IsInTransaction && command != "EXEC" && command != "DISCARD" && command != "WATCH" && command != "MULTI")
        {
            context.CommandQueue.Enqueue(parts);
            await RespWriter.WriteSimpleString(stream, "QUEUED");
            return;
        }

        if (_handlers.TryGetValue(command, out var handler))
            await handler.Handle(stream, parts, context);
        else
            await RespWriter.WriteError(stream, $"Unknown command '{command}'");
    }

}
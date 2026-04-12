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
            new TypeHandler(store)
        };
        _handlers = commands.ToDictionary(c => c.CommandName.ToString());   
    }

    public async Task Dispatch(NetworkStream stream, List<string> parts)
    {
        if (parts.Count == 0) { await RespWriter.WriteError(stream, "Empty command"); return; }

        var command = parts[0].ToUpper();
        if (_handlers.TryGetValue(command, out var handler))
            await handler.Handle(stream, parts);
        else
            await RespWriter.WriteError(stream, $"Unknown command '{command}'");
    }

}
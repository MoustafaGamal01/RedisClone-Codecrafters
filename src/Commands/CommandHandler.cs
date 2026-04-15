namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

public class CommandHandler
{
    private readonly Dictionary<string, ICommandHandler> _handlers;
    private readonly Queue<List<string>> _commandQueue = new();

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
            new MultiHandler(store),
            new ExecHandler(store),
        };

        _handlers = commands.ToDictionary(c => c.CommandName.ToString());
    }

    public async Task Dispatch(NetworkStream stream, List<string> parts)
    {
        if (parts.Count == 0) { await RespWriter.WriteError(stream, "Empty command"); return; }

        var command = parts[0].ToUpper();

        if (Store.multiState == Store.MultiState.multi)
        {
            if (command == "EXEC")
            {
                if (_commandQueue.Count > 0)
                {
                    while (_commandQueue.Count > 0)
                    {
                        var queuedParts = _commandQueue.Dequeue();
                        var queuedCommand = queuedParts[0].ToUpper();
                        if (_handlers.TryGetValue(queuedCommand, out var queuedHandler))
                            await queuedHandler.Handle(stream, queuedParts);
                        else
                            await RespWriter.WriteError(stream, $"Unknown command '{queuedCommand}'");
                    }
                }
                else
                {
                    if (_handlers.TryGetValue(command, out var handlerr))
                        await handlerr.Handle(stream, parts);
                    else
                        await RespWriter.WriteError(stream, $"Unknown command '{command}'");
                }
            }
            else
            {
                _commandQueue.Enqueue(parts);
                return;
            }
        } 
        if (_handlers.TryGetValue(command, out var handler))
            await handler.Handle(stream, parts);
        else
            await RespWriter.WriteError(stream, $"Unknown command '{command}'");
    }

}
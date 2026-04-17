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
        };

        _handlers = commands.ToDictionary(c => c.CommandName.ToString());
    }

    public async Task Dispatch(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count == 0) { await RespWriter.WriteError(stream, "Empty command"); return; }

        var command = parts[0].ToUpper();

        if (command == "MULTI")
        {
            context.IsInTransaction = true;
            context.CommandQueue.Clear();
            await RespWriter.WriteSimpleString(stream, "OK");
            return;
        }

        if (command == "EXEC")
        {
            if (!context.IsInTransaction)
            {
                await RespWriter.WriteError(stream, "EXEC without MULTI");
                return;
            }

            var queued = context.CommandQueue.ToList();
            context.CommandQueue.Clear();
            context.IsInTransaction = false;

            await RespWriter.WriteArrayHeader(stream, queued.Count);

            foreach (var queuedParts in queued)
            {
                var queuedCommand = queuedParts[0].ToUpper();
                if (_handlers.TryGetValue(queuedCommand, out var queuedHandler))
                    await queuedHandler.Handle(stream, queuedParts);
                else
                    await RespWriter.WriteError(stream, $"Unknown command '{queuedCommand}'");
            }

            return;
        }

        if (context.IsInTransaction)
        {
            context.CommandQueue.Enqueue(parts);
            await RespWriter.WriteSimpleString(stream, "QUEUED");
            return;
        }

        if (_handlers.TryGetValue(command, out var handler))
            await handler.Handle(stream, parts);
        else
            await RespWriter.WriteError(stream, $"Unknown command '{command}'");
    }

}
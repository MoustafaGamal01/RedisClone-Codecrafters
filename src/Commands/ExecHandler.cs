using System.Net.Sockets;
using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;

namespace codecrafters_redis.src.Commands;

internal class ExecHandler : ICommandHandler
{
    private readonly CommandHandler _dispatcher;
    private readonly Store _store;
    public ExecHandler(CommandHandler dispatcher, Store store)
    {
        _dispatcher = dispatcher;
        _store = store;
    }

    public CommandsName CommandName => CommandsName.EXEC;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (!context.IsInTransaction)
        {
            await RespWriter.WriteError(stream, "EXEC without MULTI");
            return;
        }

        bool isAborted = false;
        foreach (var kvp in context.WatchedKeys)
        {
            if (_store.GetVersion(kvp.Key) != kvp.Value)
            {
                isAborted = true;
                break;
            }
        }
        context.WatchedKeys.Clear();

        var queued = context.CommandQueue.ToList();
        context.CommandQueue.Clear();
        context.IsInTransaction = false;

        if(isAborted)
        {
            await RespWriter.WriteNullArray(stream);
            return;
        }

        await RespWriter.WriteArrayHeader(stream, queued.Count);

        foreach (var queuedParts in queued)
        {
            await _dispatcher.Dispatch(stream, queuedParts, context);
        }
    }
}

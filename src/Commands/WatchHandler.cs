
using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class WatchHandler : ICommandHandler
{
    private readonly Store _store;
    public WatchHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.WATCH;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(context.IsInTransaction)
        {
            await RespWriter.WriteError(stream, "WATCH inside MULTI is not allowed");
            return;
        }

        // Mark the client as being in a watched state
        for (int i = 1; i < parts.Count; i++)
        {
            var key = parts[i];
            context.WatchedKeys[key] = _store.GetVersion(key);
        }

        await RespWriter.WriteSimpleString(stream, "OK");
    }
}

using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class RPushHandler : ICommandHandler
{
    private readonly Store _store;
    public RPushHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.RPUSH;

    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "RPUSH requires a key and at least one value");
            return;
        }

        var key = parts[1];

        if (_store.TryNotifyWaiter(key, parts[2]))
        {
            await RespWriter.WriteInteger(stream, 1);
            return;
        }

        var length = _store.RPUSH(parts);
        await RespWriter.WriteInteger(stream, length);
    }

}

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

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "RPUSH requires a key and at least one value");
            return;
        }

        var key = parts[1];

        var length = _store.RPUSH(parts);
        await RespWriter.WriteInteger(stream, length);
    }

}

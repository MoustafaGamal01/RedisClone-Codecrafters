using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class LPopHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.LPOP;
    private readonly Store _store;
    public LPopHandler(Store store)
    {
        _store = store;
    }

    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        var value = _store.LPOP(parts);
        if (value is null)
            await RespWriter.WriteNullBulkString(stream);
        else if (parts.Count == 2)
            await RespWriter.WriteBulkString(stream, value[0]);
        else
            await RespWriter.WriteArray(stream, value);
    }
}

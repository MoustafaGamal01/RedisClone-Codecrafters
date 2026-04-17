using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class LLenHandler : ICommandHandler
{
    private readonly Store _store;
    public LLenHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.LLEN;

    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        var size = _store.LLEN(parts);
        await RespWriter.WriteInteger(stream, size);
    }

}

using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class TypeHandler : ICommandHandler
{
    private readonly Store _store;
    public TypeHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.TYPE;

    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        string? key = parts[1];

        var value = _store.TYPE(key);
    
        await RespWriter.WriteSimpleString(stream, value.ToString().ToLower());
    }
}

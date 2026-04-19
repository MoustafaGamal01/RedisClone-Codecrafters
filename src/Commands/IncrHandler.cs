using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class IncrHandler : ICommandHandler
{
    private readonly Store _store;
    public IncrHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.INCR;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var result = _store.INCR(parts[1]);

        if(result.Item1 == false)
        {
            await RespWriter.WriteError(stream, "value is not an integer or out of range");
            return;
        }

        await RespWriter.WriteInteger(stream, result.Item2);
    }
}

using System.Net.Sockets;
using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;

namespace codecrafters_redis.src.Commands;

internal class MultiHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.MULTI;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        context.IsInTransaction = true;
        context.CommandQueue.Clear();
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}

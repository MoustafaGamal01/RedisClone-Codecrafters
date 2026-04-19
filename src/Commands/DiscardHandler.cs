using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class DiscardHandler : ICommandHandler 
{
    public CommandsName CommandName => CommandsName.DISCARD;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (!context.IsInTransaction)
        {
            await RespWriter.WriteError(stream, "DISCARD without MULTI");
            return;
        }
        context.IsInTransaction = false;
        context.CommandQueue.Clear();
        context.WatchedKeys.Clear();
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}

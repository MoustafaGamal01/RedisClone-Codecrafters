
using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class WatchHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.WATCH;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(context.IsInTransaction)
        {
            await RespWriter.WriteError(stream, "WATCH inside MULTI is not allowed");
            return;
        }
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}

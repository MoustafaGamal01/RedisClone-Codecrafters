
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
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}

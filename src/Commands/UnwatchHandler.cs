
using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class UnwatchHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.UNWATCH;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        context.WatchedKeys.Clear();
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}


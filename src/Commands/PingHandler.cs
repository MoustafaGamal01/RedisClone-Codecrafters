using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class PingHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.PING;
    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        await RespWriter.WriteSimpleString(stream, "PONG");
    }
}

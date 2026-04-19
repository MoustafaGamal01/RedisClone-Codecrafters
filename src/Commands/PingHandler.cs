using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;
using codecrafters_redis.src.Client;

namespace codecrafters_redis.src.Commands;

internal class PingHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.PING;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        await RespWriter.WriteSimpleString(stream, "PONG");
    }
}

using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands.Key;

internal class EchoHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.ECHO;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 2)
        {
            await RespWriter.WriteError(stream, "ECHO requires an argument");
            return;
        }
        if (!context.SuppressResponses)
        {
            await RespWriter.WriteBulkString(stream, parts[1]);
        }
    }
}

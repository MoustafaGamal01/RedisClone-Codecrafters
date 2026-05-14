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
        if (context.SubscribedChannels.Count > 0)
        {
            var response = $"*2\r\n$4\r\npong\r\n${0}\r\n{""}\r\n";
            stream.Write(System.Text.Encoding.UTF8.GetBytes(response));
            return;
        }

        if (!context.SuppressResponses)
        {
            await RespWriter.WriteSimpleString(stream, "PONG");
        }
    }
}

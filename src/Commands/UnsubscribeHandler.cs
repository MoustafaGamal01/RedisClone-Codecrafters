using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class UnsubscribeHandler:ICommandHandler
{
    private readonly Store _store;
    public UnsubscribeHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.UNSUBSCRIBE;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 2)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'UNSUBSCRIBE' command");
            return;
        }

        var channel = parts[1];

        var size = _store.Unsubscribe(channel, stream);

        context.SubscribedChannels.Remove(channel);

        var response = $"*3\r\n$11\r\nunsubscribe\r\n${channel.Length}\r\n{channel}\r\n:{size}\r\n";

        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
    }
}

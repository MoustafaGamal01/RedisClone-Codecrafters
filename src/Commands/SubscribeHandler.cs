using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class SubscribeHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.SUBSCRIBE;
    private readonly Store _store;
    public SubscribeHandler(Store store)
    {
        _store = store;
    }

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var channel = parts[1];
        context.SubscribedChannels.Add(channel);
        
        _store.SUBSCRIBE(channel, stream);

        var count = context.SubscribedChannels.Count;
        var response = $"*3\r\n$9\r\nsubscribe\r\n${channel.Length}\r\n{channel}\r\n:{count}\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
    }
}

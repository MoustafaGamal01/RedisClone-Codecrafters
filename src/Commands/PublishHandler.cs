using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class PublishHandler : ICommandHandler {
    public CommandsName CommandName => CommandsName.PUBLISH;

    public readonly Store _store;
    public PublishHandler(Store store)
    {
        _store = store;
    }

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "Wrong number of arguments for 'PUBLISH' command");
            return;
        }

        var channel = parts[1];
        var count = _store.GetSubscriberCount(channel);
        await RespWriter.WriteInteger(stream, count);
    }
}

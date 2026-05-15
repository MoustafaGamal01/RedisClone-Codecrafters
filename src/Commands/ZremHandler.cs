using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class ZremHandler : ICommandHandler
{
    private readonly Store _store;
    public ZremHandler(Store store)
    {
        _store = store;
    }   
    public CommandsName CommandName => CommandsName.ZREM;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'zrem' command");
            return;
        }

        var result = _store.ZREM(parts[1], parts[2]);

        await RespWriter.WriteInteger(stream, result);
    }
}

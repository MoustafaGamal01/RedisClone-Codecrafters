using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.SortedSet;

internal class ZCardHandler : ICommandHandler
{

    private readonly Store _store;
    public ZCardHandler(Store store)
    {
        _store = store;
    }   
    public CommandsName CommandName => CommandsName.ZCARD;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count != 2)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'zcard' command\r\n");
        }
        
        var result = _store.ZCard(parts[1]);    
        await RespWriter.WriteInteger(stream, result);
    }
}

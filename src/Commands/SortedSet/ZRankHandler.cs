using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.SortedSet;

internal class ZRankHandler : ICommandHandler
{
    private readonly Store _store;
    public ZRankHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.ZRANK;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count != 3)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'zrank' command\r\n");
            return;
        }

        var result = _store.ZRank(parts[1], parts[2]);

        if (result == -1) 
            await RespWriter.WriteNullBulkString(stream);
        else
            await RespWriter.WriteInteger(stream, result);
    }
}

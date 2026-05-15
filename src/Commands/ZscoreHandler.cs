using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class ZscoreHandler : ICommandHandler
{
    private readonly Store _store;
    public ZscoreHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.ZSCORE;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments from ZSCORE command");
            return;
        }

        var result = _store.ZSCORE(parts[1], parts[2]);

        if (result == -1)
            await RespWriter.WriteNullBulkString(stream);
        else
            await RespWriter.WriteBulkString(stream, result.ToString());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.SortedSet;

internal class ZAddHandler : ICommandHandler
{
    private readonly Store _store;
    public ZAddHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.ZADD;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 4 || parts.Count % 2 != 0)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'zadd' command");
            return;
        }

        if (!double.TryParse(parts[2], out _))
        {
            await RespWriter.WriteError(stream, "value is not a valid float");
            return;
        }

        var numberOfMembers = _store.ZAdd(parts) == true ? 1 : 0;

        await RespWriter.WriteInteger(stream, numberOfMembers);
    }
}

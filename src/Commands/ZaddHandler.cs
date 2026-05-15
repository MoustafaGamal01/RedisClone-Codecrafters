using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class ZaddHandler : ICommandHandler
{
    private readonly Store _store;
    public ZaddHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.ZADD;

    public Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count < 4 || parts.Count % 2 != 0)
        {
            return RespWriter.WriteError(stream,"wrong number of arguments for 'zadd' command\r\n");
        }

        var numberOfMembers = _store.ZADD(parts);

        return RespWriter.WriteInteger(stream, numberOfMembers);
    }
}

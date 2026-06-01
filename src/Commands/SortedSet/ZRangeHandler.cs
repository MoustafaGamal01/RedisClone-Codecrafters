using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.SortedSet;

internal class ZRangeHandler : ICommandHandler
{
    private readonly Store _store;
    public ZRangeHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.ZRANGE;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 4)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'zrange' command");
            return;
        }

        var key = parts[1];
        if (!int.TryParse(parts[2], out var start) || !int.TryParse(parts[3], out var end))
        {
            await RespWriter.WriteError(stream, "value is not an integer or out of range");
            return;
        }

        var result = _store.ZRange(key, start, end);

        if(result == null || result.Count == 0) {
            await RespWriter.WriteEmptyArray(stream);
            return;
        }

        await RespWriter.WriteArray(stream, result);
    }
}

/*
 
  .\redis-cli.exe -p 6379 ZADD racer_scores 10.2 "Royce"

  .\redis-cli.exe -p 6379 ZADD racer_scores 6.0 "Ford"

  .\redis-cli.exe -p 6379 ZADD racer_scores 14.1 "Prickett"

  .\redis-cli.exe -p 6379 ZRANGE racer_scores 0 2
 
 */

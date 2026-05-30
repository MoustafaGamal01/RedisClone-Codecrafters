using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

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
        if(parts.Count < 3) {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'ZRANGE' command");
            return;
        }

        var key = parts[1];
        var start = int.Parse(parts[2]);
        var end = int.Parse(parts[3]);

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

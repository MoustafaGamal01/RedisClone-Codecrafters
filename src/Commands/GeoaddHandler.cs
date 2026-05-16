using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class GeoaddHandler : ICommandHandler
{
    private readonly Store _store;
    public GeoaddHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.GEOADD;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count < 5 || parts.Count % 2 == 0)
        {
            await RespWriter.WriteError(stream,"wrong number of arguments for GEOADD command");
            return;
        }


        var key = parts[1];
        var logitude = double.Parse(parts[2]);
        var latitude = double.Parse(parts[3]);
        var place = parts[4];

        var added = _store.GEOADD(key, logitude, latitude, place);
    
        await RespWriter.WriteInteger(stream, added);
    }
}

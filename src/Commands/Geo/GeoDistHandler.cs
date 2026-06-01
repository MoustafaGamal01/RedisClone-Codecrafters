using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.Geo;

internal class GeoDistHandler : ICommandHandler
{
    private readonly Store _store;
    public GeoDistHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.GEODIST;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 4 || parts.Count > 5)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'GEODIST' command");
            return;
        }

        var unit = parts.Count == 5 ? parts[4].ToLower() : "m";
        var result = _store.GeoDist(parts[1], parts[2], parts[3]);

        if (result == -1)
        {
            await RespWriter.WriteNullBulkString(stream);
            return;
        }

        var converted = unit switch
        {
            "m" => result,              
            "km" => result / 1000,      
            "mi" => result / 1609.344,  
            "ft" => result * 3.28084,   
            _ => result               
        };


        await RespWriter.WriteBulkString(stream, converted.ToString("F4"));
    }
}

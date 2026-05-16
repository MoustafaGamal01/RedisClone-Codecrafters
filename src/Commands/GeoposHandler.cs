using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class GeoposHandler : ICommandHandler
{
    private readonly Store _store;
    public GeoposHandler(Store store)
    {
        _store = store;
    }   
    public CommandsName CommandName => CommandsName.GEOPOS;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "wrong number of args for 'GEOPOS' command");
            return;
        }

        var key = parts[1];
        var places = parts.Skip(2).ToList(); 

        var result = _store.GEOPOS(key, places);

        var sb = new StringBuilder();
        sb.Append($"*{result.Count}\r\n");

        foreach (var item in result)
        {
            if (item == null)
            {
                sb.Append("*-1\r\n");
            }
            else
            {
                var lonStr = "0";
                var latStr = "0";
                sb.Append($"*2\r\n${lonStr.Length}\r\n{lonStr}\r\n${latStr.Length}\r\n{latStr}\r\n");
            }
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}


namespace codecrafters_redis.src.Commands.Geo;

internal class GeoAddHandler : ICommandHandler
{
    private readonly Store _store;
    public GeoAddHandler(Store store)
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

        if(logitude < -180 || logitude > 180 || latitude < -85.05112878 || latitude > 85.05112878)
        {
            await RespWriter.WriteError(stream, $"invalid longitude,latitude pair {logitude},{latitude}");
            return;
        }

        var added = _store.GeoAdd(key, logitude, latitude, place);
    
        await RespWriter.WriteInteger(stream, added);
    }
}

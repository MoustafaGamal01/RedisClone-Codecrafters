
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
        if (parts.Count < 5 || parts.Count % 2 == 0)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'geoadd' command");
            return;
        }

        var key = parts[1];
        if (!double.TryParse(parts[2], out var logitude) || !double.TryParse(parts[3], out var latitude))
        {
            await RespWriter.WriteError(stream, "value is not a valid float");
            return;
        }
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

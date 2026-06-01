namespace codecrafters_redis.src.Commands.Geo;

internal class GeoSearchHandler : ICommandHandler
{
    private readonly Store _store;
    public GeoSearchHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.GEOSEARCH;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 7)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'geosearch' command");
            return;
        }

        var key = parts[1];
        if (!double.TryParse(parts[3], out var lon) || 
            !double.TryParse(parts[4], out var lat) || 
            !double.TryParse(parts[6], out var dis))
        {
            await RespWriter.WriteError(stream, "value is not a valid float");
            return;
        }

        var result = _store.GeoSearch(key, lon, lat, dis);

        await RespWriter.WriteArray(stream, result);

    }
}

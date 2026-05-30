namespace codecrafters_redis.src.Commands;

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
        // > GEOSEARCH places FROMLONLAT 2 48 BYRADIUS 100 m
        var key = parts[1];
        var lon = double.Parse(parts[3]);
        var lat = double.Parse(parts[4]);
        var dis = double.Parse(parts[6]);

        var result = _store.GeoSearch(key, lon, lat, dis);

        await RespWriter.WriteArray(stream, result);

    }
}

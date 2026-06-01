namespace codecrafters_redis.src.Commands.Stream;

internal class XRangeHandler : ICommandHandler
{
    private readonly Store _store;
    public XRangeHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.XRANGE;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 4)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'xrange'");
            return;
        }

        var key = parts[1];
        var start = parts[2];
        var end = parts[3];

        var result = _store.XRange(key, start, end);
        await RespWriter.WriteXRange(stream, result);
    }
}

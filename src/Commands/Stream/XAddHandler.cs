
namespace codecrafters_redis.src.Commands.Stream;

internal class XAddHandler : ICommandHandler
{
    private readonly Store _store;
    public XAddHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.XADD;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var key = parts[1];
        var id = parts[2];

        var fields = new Dictionary<string, string>();
        for (int i = 3; i < parts.Count; i += 2)
        {
            var fieldKey = parts[i];
            var fieldValue = (i + 1 < parts.Count) ? parts[i + 1] : "";
            fields[fieldKey] = fieldValue;
        }

        var (success, resolvedId) = _store.XAdd(key, id, fields);
        if (!success)
        {
            await RespWriter.WriteError(stream, resolvedId);
            return;
        }

        if (!context.SuppressResponses)
        {
            await RespWriter.WriteBulkString(stream, resolvedId);
        }
    }
}


namespace codecrafters_redis.src.Commands.Key;

internal class TypeHandler : ICommandHandler
{
    private readonly Store _store;
    public TypeHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.TYPE;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        string? key = parts[1];

        var value = _store.Type(key);
    
        await RespWriter.WriteSimpleString(stream, value.ToString().ToLower());
    }
}

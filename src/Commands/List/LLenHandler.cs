
namespace codecrafters_redis.src.Commands.List;

internal class LLenHandler : ICommandHandler
{
    private readonly Store _store;
    public LLenHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.LLEN;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var size = _store.LLen(parts);
        await RespWriter.WriteInteger(stream, size);
    }

}


namespace codecrafters_redis.src.Commands.Transaction;

internal class UnwatchHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.UNWATCH;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        context.WatchedKeys.Clear();
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}


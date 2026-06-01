
namespace codecrafters_redis.src.Commands.Transaction;

internal class MultiHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.MULTI;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        context.IsInTransaction = true;
        context.CommandQueue.Clear();
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}

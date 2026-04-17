using System.Net.Sockets;
using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;

namespace codecrafters_redis.src.Commands;

internal class ExecHandler : ICommandHandler
{
    private readonly CommandHandler _dispatcher;

    public ExecHandler(CommandHandler dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public CommandsName CommandName => CommandsName.EXEC;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (!context.IsInTransaction)
        {
            await RespWriter.WriteError(stream, "EXEC without MULTI");
            return;
        }

        var queued = context.CommandQueue.ToList();
        context.CommandQueue.Clear();
        context.IsInTransaction = false;

        await RespWriter.WriteArrayHeader(stream, queued.Count);

        foreach (var queuedParts in queued)
        {
            await _dispatcher.Dispatch(stream, queuedParts, context);
        }
    }
}

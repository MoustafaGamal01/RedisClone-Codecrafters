using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class BLPopHandler : ICommandHandler
{
    private readonly Store _store;
    public BLPopHandler(Store store)
    {
        _store = store; 
    }

    public CommandsName CommandName => CommandsName.BLPOP;

    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "BLPOP requires a key and timeout");
            return;
        }

        var key = parts[1];
        var timeout = double.Parse(parts[parts.Count - 1]);

        var existing = _store.LPOP(new List<string> { "LPOP", key });
        if (existing != null)
        {
            await RespWriter.WriteArray(stream, new List<string> { key, existing[0] });
            return;
        }

        var waitTask = _store.BLPOP(key);

        if (timeout == 0)
        {
            var value = await waitTask;
            await RespWriter.WriteArray(stream, new List<string> { key, value });
            return;
        }

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout));
        var winner = await Task.WhenAny(waitTask, timeoutTask);

        if (winner == timeoutTask)
        {
            await RespWriter.WriteNullArray(stream);
        }
        else
        {
            var value = await waitTask;
            await RespWriter.WriteArray(stream, new List<string> { key, value });
        }
    }

}

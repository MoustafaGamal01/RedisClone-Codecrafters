using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.Key;

internal class KeysHandler : ICommandHandler
{
    private readonly Store _store;
    public KeysHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.KEYS;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count != 2)
        {
            await RespWriter.WriteError(stream, "ERR wrong number of arguments for 'KEYS' command");
            return;
        }

        var keys = _store.Keys(parts[1]);
        await RespWriter.WriteArray(stream, keys);
    }
}

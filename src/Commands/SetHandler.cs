using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class SetHandler : ICommandHandler
{

    public CommandsName CommandName => CommandsName.SET;
    private readonly Store _store;

    public SetHandler(Store store)
    {
        _store = store;
    }

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "SET requires key and value");
            return;
        }

        DateTime? expiresAt = null;

        // Parse optional EX / PX flags: SET key value [EX seconds | PX milliseconds]
        if (parts.Count >= 5)
        {
            var option = parts[3].ToUpper();
            if (int.TryParse(parts[4], out var amount))
            {
                expiresAt = option switch
                {
                    "EX" => DateTime.UtcNow.AddSeconds(amount),
                    "PX" => DateTime.UtcNow.AddMilliseconds(amount),
                    _ => null
                };
            }

        }
        var result = _store.Set(parts[1], parts[2], expiresAt);

        await RespWriter.WriteSimpleString(stream, "OK");

    }
}
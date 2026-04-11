namespace RedisSharp;

using System.Net.Sockets;

public class CommandHandler
{
    private readonly Store _store;

    public CommandHandler(Store store)
    {
        _store = store;
    }

    public async Task Handle(NetworkStream stream, List<string> parts)
    {
        if (parts.Count == 0)
        {
            await RespWriter.WriteError(stream, "Invalid request");
            return;
        }

        var command = parts[0].ToUpper();

        switch (command)
        {
            case "PING":
                await HandlePing(stream);
                break;

            case "ECHO":
                await HandleEcho(stream, parts);
                break;

            case "SET":
                await HandleSet(stream, parts);
                break;

            case "GET":
                await HandleGet(stream, parts);
                break;

            case "RPUSH":
                await HandleRPUSH(stream, parts);
                break;

            case "LRANGE":  
                await HandleLRANGE(stream, parts);
                break;

            case "LPUSH":
                await HandleLPUSH(stream, parts);
                break;

            case "LLEN":
                await HandleLLEN(stream, parts);
                break;

            case "LPOP":
                await HandleLPOP(stream, parts);
                break;

            // default case for unknown commands
            default:
                await RespWriter.WriteError(stream, $"Unknown command '{command}'");
                break;
        }
    }

    private static async Task HandlePing(NetworkStream stream)
    {
        await RespWriter.WriteSimpleString(stream, "PONG");
    }

    private static async Task HandleEcho(NetworkStream stream, List<string> parts)
    {
        if (parts.Count < 2)
        {
            await RespWriter.WriteError(stream, "ECHO requires an argument");
            return;
        }
        await RespWriter.WriteBulkString(stream, parts[1]);
    }

    private async Task HandleSet(NetworkStream stream, List<string> parts)
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

        _store.Set(parts[1], parts[2], expiresAt);
        await RespWriter.WriteSimpleString(stream, "OK");
    }

    private async Task HandleGet(NetworkStream stream, List<string> parts)
    {
        if (parts.Count < 2)
        {
            await RespWriter.WriteError(stream, "GET requires a key");
            return;
        }

        var value = _store.Get(parts[1]);

        if (value is null)
            await RespWriter.WriteNullBulkString(stream);
        else
            await RespWriter.WriteBulkString(stream, value);
    }

    private async Task HandleRPUSH(NetworkStream stream, List<string> parts)
    {

        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "RPUSH requires a key and at least one value");
            return;
        }

        var length = _store.RPUSH(parts);

        await RespWriter.WriteInteger(stream, length);
    }

    private async Task HandleLRANGE(NetworkStream stream, List<string> parts)
    {

        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "LRANGE requires more arguments");
            return;
        }

        if (!int.TryParse(parts[2], out var start) || !int.TryParse(parts[3], out var stop))
        {
            await RespWriter.WriteError(stream, "LRANGE start and stop must be integers");
            return;
        }

        var list = _store.LRANGE(parts, start, stop);

        if (list.Count == 0)
        {
            await RespWriter.WriteEmptyArray(stream);
            return;
        }

        // for debugging
        Console.WriteLine(list.Count);
        foreach (var item in list)
        {
            Console.WriteLine(item);
        }

        await RespWriter.WriteArray(stream, list);
    }

    private async Task HandleLPUSH(NetworkStream stream, List<string> parts)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "LPUSH requires a key and at least one value");
            return;
        }

        var length = _store.LPUSH(parts);

        await RespWriter.WriteInteger(stream, length);
    }

    private async Task HandleLLEN(NetworkStream stream, List<string> parts)
    {
        var size = _store.LLEN(parts);
        await RespWriter.WriteInteger(stream, size);
    }


    private async Task HandleLPOP(NetworkStream stream, List<string> parts)
    {
        var value = _store.LPOP(parts);
        if (value is null)
            await RespWriter.WriteNullBulkString(stream);
        else if(parts.Count == 2)
            await RespWriter.WriteBulkString(stream, value[0]);
        else 
            await RespWriter.WriteArray(stream, value);
    }
}
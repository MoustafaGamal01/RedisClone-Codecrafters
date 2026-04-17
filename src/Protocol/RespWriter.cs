namespace codecrafters_redis.src.Protocol;

using System.Net.Sockets;
using System.Text;

public static class RespWriter
{
    public static async Task WriteSimpleString(NetworkStream stream, string message)
    {
        var bytes = Encoding.UTF8.GetBytes($"+{message}\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteBulkString(NetworkStream stream, string message)
    {
        var bytes = Encoding.UTF8.GetBytes($"${message.Length}\r\n{message}\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteNullBulkString(NetworkStream stream)
    {
        var bytes = Encoding.UTF8.GetBytes("$-1\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteError(NetworkStream stream, string message)
    {
        var bytes = Encoding.UTF8.GetBytes($"-ERR {message}\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteInteger(NetworkStream stream , int? num)
    {
        var bytes = Encoding.UTF8.GetBytes($":{num}\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteArray(NetworkStream stream, List<string> items)
    {
        var sb = new StringBuilder();
        sb.Append($"*{items.Count}\r\n");
        foreach (var item in items)
        {
            sb.Append($"${item.Length}\r\n{item}\r\n");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteArrayHeader(NetworkStream stream, int count)
    {
        var bytes = Encoding.UTF8.GetBytes($"*{count}\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteEmptyArray(NetworkStream stream)
    {
        var message = "*0\r\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteNullArray(NetworkStream stream)
    {
        var bytes = Encoding.UTF8.GetBytes("*-1\r\n");
        await stream.WriteAsync(bytes);
    }

    public static async Task WriteXRange(NetworkStream stream, List<(string Id, Dictionary<string, string> Fields)> entries)
    {
        var sb = new StringBuilder();
        sb.Append($"*{entries.Count}\r\n");

        foreach (var entry in entries)
        {
            sb.Append("*2\r\n");
            sb.Append($"${entry.Id.Length}\r\n{entry.Id}\r\n");

            sb.Append($"*{entry.Fields.Count * 2}\r\n");

            foreach (var field in entry.Fields)
            {
                sb.Append($"${field.Key.Length}\r\n{field.Key}\r\n");

                sb.Append($"${field.Value.Length}\r\n{field.Value}\r\n");
            }
        }

        var data = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(data, 0, data.Length);
    }

    public static async Task WriteXRead(
    NetworkStream stream,
    List<(string StreamKey, List<(string Id, Dictionary<string, string> Fields)> Entries)> streams)
    {
        var sb = new StringBuilder();

        sb.Append($"*{streams.Count}\r\n");

        foreach (var streamEntry in streams)
        {
            sb.Append("*2\r\n");

            // stream name
            sb.Append($"${streamEntry.StreamKey.Length}\r\n{streamEntry.StreamKey}\r\n");

            // entries array
            sb.Append($"*{streamEntry.Entries.Count}\r\n");

            foreach (var entry in streamEntry.Entries)
            {
                sb.Append("*2\r\n");

                // id
                sb.Append($"${entry.Id.Length}\r\n{entry.Id}\r\n");

                // fields
                sb.Append($"*{entry.Fields.Count * 2}\r\n");

                foreach (var field in entry.Fields)
                {
                    sb.Append($"${field.Key.Length}\r\n{field.Key}\r\n");
                    sb.Append($"${field.Value.Length}\r\n{field.Value}\r\n");
                }
            }
        }

        var data = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(data, 0, data.Length);
    }

}
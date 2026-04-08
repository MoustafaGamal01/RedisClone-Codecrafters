using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;

var listerner = new TcpListener(IPAddress.Any, 6379);
listerner.Start();

ConcurrentDictionary<string, (string Value, DateTime? ExpiresAt)> stringSetter = new();

while (true)
{
    var client = await listerner.AcceptTcpClientAsync();

    _ = Task.Run(async () =>
    {
        var stream = client.GetStream();

        var buffer = new byte[1024];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Client disconnected");
                break;
            }
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            var parts = ParseRESP(request);

            parts.ForEach(p => Console.WriteLine($"Part: {p}"));


            if (parts.Count == 0)
            {
                await WriteError(stream, "Invalid request");
                continue;
            }

            var cmnd = parts[0].ToUpper();
            switch (cmnd)
            {
                case "PING":
                    await WriteSimpleString(stream, "PONG");
                    break;
              
                case "ECHO":
                    if (parts.Count < 2) await WriteError(stream, "ECHO requires argument");
                    else await WriteBulkString(stream, parts[1]);
                    break;

                case "SET":
                    if (parts.Count < 3)
                    {
                        await WriteError(stream, "SET requires argument");
                        break;
                    }
                    DateTime? expiresAt = null;
                    if (parts.Count >= 5)
                    {
                        var option = parts[3].ToUpper();
                        var amount = int.Parse(parts[4]);
                        expiresAt = option switch
                        {
                            "PX" => DateTime.UtcNow.AddMilliseconds(amount),
                            "EX" => DateTime.UtcNow.AddSeconds(amount),
                            _ => null
                        };
                    }
                    stringSetter[parts[1]] = (parts[2], expiresAt);
                    await WriteSimpleString(stream, "OK");
                    break;

                case "GET":
                    if (parts.Count < 2)
                    {
                        await WriteError(stream, "GET requires argument");
                        break;
                    }
                    if (!stringSetter.TryGetValue(parts[1], out var entry))
                    {
                        await NullBulkString(stream);
                        break;
                    }
                    if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
                    {
                        stringSetter.TryRemove(parts[1], out _);
                        await NullBulkString(stream);
                        break;
                    }
                    await WriteBulkString(stream, entry.Value);
                    break;
                default:
                    await WriteError(stream, "Unknown command");
                    break;
            }
        }
    });
}

List<string> ParseRESP(string request) 
{
    // *2\r\n$4\r\nECHO\r\n$3\r\nhey\r\n
    List<string> parts = new List<string>();
    
    // Split by (\r\n)
    var lines = request.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        
    if(lines.Length < 1)
    {

    }
    
    foreach(string line in lines)
    {
        if(line.StartsWith("$") || line.StartsWith("*"))
        {
            // Bulk string length, skip
            continue;
        }
        parts.Add(line);        
    }
    
    
    return parts;
}

async Task WriteError(NetworkStream stream, string message)
{
    var errorMessage = $"-ERR {message}\r\n";
    var bytes = Encoding.UTF8.GetBytes(errorMessage);
    await stream.WriteAsync(bytes);
}

async Task WriteSimpleString(NetworkStream stream, string message)
{
    var response = $"+{message}\r\n";
    var bytes = Encoding.UTF8.GetBytes(response);
    await stream.WriteAsync(bytes);
}

async Task WriteBulkString(NetworkStream stream, string message)
{
    var response = $"${message.Length}\r\n{message}\r\n";
    var bytes = Encoding.UTF8.GetBytes(response);
    await stream.WriteAsync(bytes);
}

async Task NullBulkString(NetworkStream stream)
{
    var nullString = Encoding.UTF8.GetBytes("$-1\r\n");
    await stream.WriteAsync(nullString);
}

/*
 * 3 STEPS ================> 
 * 1- GET THE NORMAL STRING (each with it's own style)
 * 2- ENCODE IT TO UTF-8
 * 3- WRITE IT TO THE STREAM
 */
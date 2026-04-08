using System.Collections.Specialized;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;

var listerner = new TcpListener(IPAddress.Any, 6379);
listerner.Start();

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
                    if (parts.Count < 2)
                    {
                        await WriteError(stream, "ECHO requires argument");
                    }
                    else
                    {
                        await WriteBulkString(stream, parts[1]);
                    }
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
    
    var result = new List<string>();

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
using System.Net;
using System.Net.Sockets;
using System.Text;
using RedisSharp;

var listener = new TcpListener(IPAddress.Any, 6379);
listener.Start();
Console.WriteLine("RedisSharp listening on port 6379...");

var store = new Store();
var handler = new CommandHandler(store);

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    Console.WriteLine("Client connected.");

    _ = Task.Run(async () =>
    {
        var stream = client.GetStream();
        var buffer = new byte[40096];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Client disconnected.");
                break;
            }

            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var parts = RespParser.Parse(request);

            await handler.Handle(stream, parts);
        }
    });
}

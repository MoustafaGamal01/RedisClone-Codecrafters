using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Core;
using System.Net;
using System.Net.Sockets;


class Program
{
    static async Task Main(string[] args)
    {
        int port = 6379; // default

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                port = int.Parse(args[i + 1]);
            }
        }

        var store = new Store();
        var dispatcher = new CommandHandler(store);
        var clientHandler = new ClientHandler(dispatcher, args);
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => clientHandler.HandleAsync(client));
        }

    }
}
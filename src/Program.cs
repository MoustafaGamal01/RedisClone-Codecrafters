using codecrafters_redis.src.Commands;
using codecrafters_redis.src.RedisList;
using RedisSharp;
using System.Net;
using System.Net.Sockets;

var store = new Store();
var dispatcher = new CommandHandler(store);
var clientHandler = new ClientHandler(dispatcher);
var listener = new TcpListener(IPAddress.Any, 6379);
listener.Start();

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => clientHandler.HandleAsync(client));
}

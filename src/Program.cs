using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Core;
using System.Net;
using System.Net.Sockets;


class Program
{
    static async Task Main(string[] args)
    {
        await new ServerBoot(args).RunAsync();
    }
}
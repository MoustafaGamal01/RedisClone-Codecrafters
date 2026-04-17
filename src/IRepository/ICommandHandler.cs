using codecrafters_redis.src.Commands;
using System.Net.Sockets;

namespace codecrafters_redis.src.IRepository;

internal interface ICommandHandler
{
    CommandsName CommandName { get; }

    Task Handle(NetworkStream stream, List<string> parts);
}

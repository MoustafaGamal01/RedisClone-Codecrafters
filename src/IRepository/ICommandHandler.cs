using codecrafters_redis.src.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.IRepository
{
    internal interface ICommandHandler
    {
        CommandsName CommandName { get; }

        Task Handle(NetworkStream stream, List<string> parts);
    }
}

using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands
{
    internal class PingHandler : ICommandHandler
    {
        public CommandsName CommandName => CommandsName.PING;
        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            await RespWriter.WriteSimpleString(stream, "PONG");
        }
    }
}

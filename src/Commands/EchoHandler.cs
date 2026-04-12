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
    internal class EchoHandler : ICommandHandler
    {
        public CommandsName CommandName => CommandsName.ECHO;

        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            if (parts.Count < 2)
            {
                await RespWriter.WriteError(stream, "ECHO requires an argument");
                return;
            }
            await RespWriter.WriteBulkString(stream, parts[1]);
        }
    }
}

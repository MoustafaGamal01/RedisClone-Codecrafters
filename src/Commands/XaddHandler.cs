using codecrafters_redis.src.Core;
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
    internal class XaddHandler : ICommandHandler
    {
        private readonly Store _store;
        public XaddHandler(Store store)
        {
            _store = store;
        }

        public CommandsName CommandName => CommandsName.XADD;

        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            _store.XADD(parts[1],   parts[2], parts.Skip(3).ToDictionary(k => k, v => v));

            await RespWriter.WriteBulkString(stream, parts[2]);
        }
    }
}

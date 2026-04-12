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
            var result = _store.XADD(parts[1], parts[2], parts.Skip(3).ToDictionary(k => k, v => v));

            if(result.Value.Item1 == false)
                await RespWriter.WriteError(stream, result.Value.Item2);
            else
                await RespWriter.WriteBulkString(stream, result.Value.Item2);

        }
    }
}

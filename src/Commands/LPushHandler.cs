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
    internal class LPushHandler : ICommandHandler
    {
        private readonly Store _store;
        public LPushHandler(Store store)
        {
            _store = store;
        }

        public CommandsName CommandName => CommandsName.LPUSH;

        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            if (parts.Count < 3)
            {
                await RespWriter.WriteError(stream, "LPUSH requires a key and at least one value");
                return;
            }

            var length = _store.LPUSH(parts);

            await RespWriter.WriteInteger(stream, length);
        }
    }
}

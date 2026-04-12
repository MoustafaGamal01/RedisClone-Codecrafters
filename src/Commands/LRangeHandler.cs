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
    internal class LRangeHandler : ICommandHandler
    {
        public CommandsName CommandName => CommandsName.LRANGE;
        private readonly Store _store;
        public LRangeHandler(Store store)
        {
            _store = store;
        }

        public async Task Handle(NetworkStream stream, List<string> parts)
        {

            if (parts.Count < 3)
            {
                await RespWriter.WriteError(stream, "LRANGE requires more arguments");
                return;
            }

            if (!int.TryParse(parts[2], out var start) || !int.TryParse(parts[3], out var stop))
            {
                await RespWriter.WriteError(stream, "LRANGE start and stop must be integers");
                return;
            }

            var list = _store.LRANGE(parts, start, stop);

            if (list.Count == 0)
            {
                await RespWriter.WriteEmptyArray(stream);
                return;
            }

            // for debugging
            Console.WriteLine(list.Count);
            foreach (var item in list)
            {
                Console.WriteLine(item);
            }

            await RespWriter.WriteArray(stream, list);
        }

    }
}

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
    internal class XReadHandler : ICommandHandler

    {
        private readonly Store _store;
        public XReadHandler(Store store)
        {
            _store = store;
        }

        public CommandsName CommandName => CommandsName.XREAD;

        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            int blockMs = -1;
            int index = 1;

            if (index < parts.Count && parts[index].Equals("BLOCK", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= parts.Count || !int.TryParse(parts[index + 1], out blockMs))
                {
                    await RespWriter.WriteError(stream, "ERR invalid BLOCK value");
                    return;
                }

                index += 2;
            }

            if (index >= parts.Count || !parts[index].Equals("STREAMS", StringComparison.OrdinalIgnoreCase))
            {
                await RespWriter.WriteError(stream, "ERR syntax error");
                return;
            }

            index++;

            var remaining = parts.Count - index;

            if (remaining <= 0 || remaining % 2 != 0)
            {
                await RespWriter.WriteError(stream, "ERR Unbalanced XREAD list of streams");
                return;
            }

            int size = remaining / 2;

            var keys = parts.Skip(index).Take(size).ToList();
            var ids = parts.Skip(index + size).Take(size).ToList();

            var result = await _store.XRead(keys, ids, blockMs);

            if (result.Count == 0)
            {
                await RespWriter.WriteNullArray(stream);
                return;
            }

            await RespWriter.WriteXRead(stream, result);
        }

    }
}

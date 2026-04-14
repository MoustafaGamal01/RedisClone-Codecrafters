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
            int size = (parts.Count - 2) / 2;

            var keys = parts.Skip(2).Take(size).ToList();
            var ids = parts.Skip(2 + size).Take(size).ToList();

            if (keys.Count != ids.Count)
            {
                await RespWriter.WriteError(stream, "the number of keys and ids must be the same");
                return;
            }

            // 🔥 collect all results first
            var response = new List<(string StreamKey, List<(string Id, Dictionary<string, string> Fields)> Entries)>();

            for (int i = 0; i < keys.Count; i++)
            {
                var entries = _store.XREAD(keys[i], ids[i]);

                if (entries.Count > 0) // Redis بيرجع بس اللي فيه data
                {
                    response.Add((keys[i], entries));
                }
            }

            await RespWriter.WriteXRead(stream, response);
        }

    }
}

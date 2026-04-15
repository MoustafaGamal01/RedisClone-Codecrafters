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
    internal class IncrHandler : ICommandHandler
    {
        private readonly Store _store;
        public IncrHandler(Store store)
        {
            _store = store;
        }
        public CommandsName CommandName => CommandsName.INCR;
        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            var result = _store.INCR(parts[1]);

            await RespWriter.WriteInteger(stream, result);

        }

    }
}

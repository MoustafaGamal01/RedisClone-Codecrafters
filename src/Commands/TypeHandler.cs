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
    internal class TypeHandler : ICommandHandler
    {
        private readonly Store _store;
        public TypeHandler(Store store)
        {
            _store = store;
        }

        public CommandsName CommandName => CommandsName.TYPE;

        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            string? key = parts[1];

            var value = _store.Type(key);
        
            await RespWriter.WriteSimpleString(stream, value);
        }
    }
}

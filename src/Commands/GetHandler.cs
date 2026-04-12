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
    internal class GetHandler : ICommandHandler
    {
        public CommandsName CommandName => CommandsName.GET;
        private readonly Store _store;
        public GetHandler(Store store)
        {
            _store = store;
        }
        
        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            if (parts.Count < 2)
            {
                await RespWriter.WriteError(stream, "GET requires a key");
                return;
            }

            var value = _store.Get(parts[1]);

            if (value is null)
                await RespWriter.WriteNullBulkString(stream);
            else
                await RespWriter.WriteBulkString(stream, value.ToString().ToLower());
        }
    }
}

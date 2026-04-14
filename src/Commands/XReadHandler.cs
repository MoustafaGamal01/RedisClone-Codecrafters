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
            if(parts.Count != 4)
            {
                await RespWriter.WriteError(stream, "wrong number of arguments for 'XREAD' command");
                return;
            }

            var result = _store.XREAD(parts[2], parts[3]);

            var resultList = result.Select(r => (StreamKey: parts[2], Id: r.Id, Fields: r.Fields)).ToList();

            await RespWriter.WriteXRead(stream, resultList);
        
        }
    }
}

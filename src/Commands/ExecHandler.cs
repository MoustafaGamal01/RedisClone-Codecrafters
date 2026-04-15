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
    internal class ExecHandler : ICommandHandler
    {
        private readonly Store _store;
        public ExecHandler(Store store)
        {
            _store = store;
        }

        public CommandsName CommandName => CommandsName.EXEC;

        public async Task Handle(NetworkStream stream, List<string> parts)
        {
            var result = _store.EXEC(); 

            if(result == Store.MultiState.exec)
            {
                await RespWriter.WriteArray(stream, new List<string>());
            }
            else
                if(Store.MultiState.none == result) await RespWriter.WriteError(stream, "EXEC without MULTI");
        }
    }
}

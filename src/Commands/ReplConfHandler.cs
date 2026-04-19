using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class ReplConfHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.REPLCONF;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        await RespWriter.WriteSimpleString(stream, "OK");
    }
}


using codecrafters_redis.src.Client;
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
        if (parts.Count >= 3 && parts[1].ToUpper() == "GETACK")
        {
            var items = new List<string>() { "REPLCONF", "ACK", "0" };
            await RespWriter.WriteArray(stream, items);
            return;
        }

        if (!context.SuppressResponses)
        {
            await RespWriter.WriteSimpleString(stream, "OK");
        }
    }
}


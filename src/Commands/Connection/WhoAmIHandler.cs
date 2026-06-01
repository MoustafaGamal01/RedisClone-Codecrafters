using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.Connection;

internal class WhoAmIHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.WHOAMI;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count != 2)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'whoami' command");
            return;
        }

        await RespWriter.WriteBulkString(stream, context.AuthenticatedUser ?? "default");
    }
}

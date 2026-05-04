using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class WaitHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.WAIT;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "WAIT requires at least 3 arguments");
            return;
        }

        await RespWriter.WriteInteger(stream, 0);
    }
}

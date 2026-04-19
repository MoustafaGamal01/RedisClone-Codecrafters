using codecrafters_redis.src.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class PsyncHandler  : ICommandHandler
{
    public CommandsName CommandName => CommandsName.PSYNC;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context) 
    {
        if (parts.Count != 3)
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes("wrong number of arguments for 'psync' command\r\n"));
            return;
        }
        string runId = "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb";
        long offset = 0;

        await stream.WriteAsync(Encoding.UTF8.GetBytes($"+FULLRESYNC {runId} {offset}\r\n"));

        await RespWriter.WriteRDBFile(stream, EmptyRdb);

    }

    private static readonly byte[] EmptyRdb = Convert.FromBase64String(
    "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==");

}

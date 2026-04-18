
using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class InfoHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.INFO;

    private string GetReplicationInfo()
    {
        return "# Replication\r\nrole:master\r\nconnected_slaves:0";
    }

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var key = parts.Count > 1 ? parts[1] : "default";

        switch (key.ToUpper())
        {
            case "REPLICATION":
                await RespWriter.WriteBulkString(stream, GetReplicationInfo());
                break;
            default:
                await RespWriter.WriteError(stream, $"Unsupported INFO section: {key}");
                break;
        }
    }
}

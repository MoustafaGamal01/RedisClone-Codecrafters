
using codecrafters_redis.src.Core;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class InfoHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.INFO;

    private string GetReplicationInfo(string role, int slaveCount)
    {
        return $"# Replication\r\nrole:{role}\r\nconnected_slaves:{slaveCount}";
    }

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var key = parts.Count > 1 ? parts[1] : "default";
        var role = context.ClientRole["role"];
        var slaveCount = context.slaveCount;

        switch (key.ToUpper())
        {
            case "REPLICATION":
                await RespWriter.WriteBulkString(stream, GetReplicationInfo(role, slaveCount));
                break;
            default:
                await RespWriter.WriteError(stream, $"Unsupported INFO section: {key}");
                break;
        }
    }
}

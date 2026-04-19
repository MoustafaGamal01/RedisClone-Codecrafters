
using codecrafters_redis.src.Client;
using codecrafters_redis.src.IRepository;
using codecrafters_redis.src.Protocol;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.Net.Sockets;

namespace codecrafters_redis.src.Commands;

internal class InfoHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.INFO;

    private string GetMasterReplicationInfo(string role, int slaveCount)
    {
        return $"# Replication\r\nrole:{role}\r\nconnected_slaves:{slaveCount}\r\n" +
            $"master_replid:8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb\r\nmaster_repl_offset:0";
    }

    private string GetSlaveReplicationInfo(string role, int slaveCount)
    {
        return $"# Replication\r\nrole:{role}\r\nconnected_slaves:{slaveCount}\r\n";
    }

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var key = parts.Count > 1 ? parts[1] : "default";
        var role = context.ClientRole["role"];
        var slaveCount = context.slaveCount;
        var handShake = new List<string> { "PING" };
        switch (key.ToUpper())
        {
            case "REPLICATION":
                if (role == "master")
                    await RespWriter.WriteBulkString(stream, GetMasterReplicationInfo(role, slaveCount));
                else
                {
                    await RespWriter.WriteBulkString(stream, GetSlaveReplicationInfo(role, slaveCount));
                    await RespWriter.WriteArray(stream, handShake);
                }
                break;
            default:
                await RespWriter.WriteError(stream, $"Unsupported INFO section: {key}");
                break;
        }
    }
}

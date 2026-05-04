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
        if (context.Replication is not Master master)
        {
            await RespWriter.WriteInteger(stream, 0);
            return;
        }

        if (!int.TryParse(parts[1], out var requiredReplicas) || !int.TryParse(parts[2], out var timeout))
        {
            await RespWriter.WriteError(stream, "Invalid arguments for WAIT");
            return;
        }

        if (master.ReplOffset == 0)
        {
            await RespWriter.WriteInteger(stream, master.ConnectedReplicaCount);
            return;
        }

        await master.SendGetAck();

        var startTime = DateTime.UtcNow;
        int syncedCount = master.GetSyncedReplicaCount();

        while (syncedCount < requiredReplicas && (DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
        {
            await Task.Delay(20); 
            syncedCount = master.GetSyncedReplicaCount();
        }

        await RespWriter.WriteInteger(stream, syncedCount);
    }
}

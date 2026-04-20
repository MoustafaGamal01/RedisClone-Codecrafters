namespace codecrafters_redis.src.Commands;

internal class PsyncHandler  : ICommandHandler
{
    public CommandsName CommandName => CommandsName.PSYNC;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        Console.WriteLine("[DEBUG] PSYNC received");
        Console.WriteLine($"[DEBUG] Replication is: {context.Replication?.GetType().Name ?? "NULL"}");

        if (context.Replication is not Master master)
        {
            await RespWriter.WriteError(stream, "PSYNC only valid on master");
            return;
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes($"+FULLRESYNC {master.ReplId} {master.ReplOffset}\r\n"));
        await RespWriter.WriteRDBFile(stream, EmptyRdb);
        master.RegisterReplica(stream);
    }

    private static readonly byte[] EmptyRdb = Convert.FromBase64String(
    "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==");

}

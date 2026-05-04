internal class ReplConfHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.REPLCONF;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count >= 3 && parts[1].ToUpper() == "GETACK")
        {
            var items = new List<string>
            {
                "REPLCONF",
                "ACK",
                context.ReplicationOffset.ToString()
            };

            await RespWriter.WriteArray(stream, items);
            return;
        }

        if (!context.SuppressResponses)
        {
            await RespWriter.WriteSimpleString(stream, "OK");
        }
    }
}
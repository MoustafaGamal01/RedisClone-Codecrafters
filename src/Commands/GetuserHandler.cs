
namespace codecrafters_redis.src.Commands;

internal class GetuserHandler : ICommandHandler
{
    public CommandsName CommandName => CommandsName.GETUSER;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if(parts.Count != 3)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'getuser' command");
            return;
        }

        await RespWriter.WriteNestedArray(stream, new List<object>
        {
            "flags",
            new List<string>(),
        });

    }
}

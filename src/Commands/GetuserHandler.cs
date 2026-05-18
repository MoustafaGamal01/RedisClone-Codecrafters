
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

        var flags = "flags";

        var answer = new List<string>();

        if(parts[2] == "default") answer.Add("nopass");

        await RespWriter.WriteNestedArray(stream, new List<object>
        {
            flags,
            answer,
        });

    }
}

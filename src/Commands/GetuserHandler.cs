
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

        var pass = new List<string>();

        var passwords = new List<string>();

        if(parts[2] == "default") pass.Add("nopass");

        await RespWriter.WriteNestedArray(stream, new List<object>
        {
            "flags",
            pass,
            "passwords",
            passwords
        });

    }
}

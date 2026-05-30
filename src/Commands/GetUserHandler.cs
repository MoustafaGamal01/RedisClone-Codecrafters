using System.Net.Sockets;
using codecrafters_redis.src.Client;
using codecrafters_redis.src.Core;
using codecrafters_redis.src.Protocol;

namespace codecrafters_redis.src.Commands;

internal class GetUserHandler : ICommandHandler
{
    private readonly Store _store;

    public GetUserHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.GETUSER;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "wrong number of arguments for 'ACL GETUSER' command");
            return;
        }

        var username = parts[2];
        var passwords = _store.GetUserPasswords(username);

        var pass = new List<string>();

        if (passwords.Count == 0) pass.Add("nopass");

        await RespWriter.WriteNestedArray(stream, new List<object>
        {
            "flags",
            pass,
            "passwords",
            passwords,
        });
    }
}


using System.Security.Cryptography;

namespace codecrafters_redis.src.Commands;

internal class SetUserHandler : ICommandHandler
{
    private readonly Store _store;

    public SetUserHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.SETUSER;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3)
        {
            await RespWriter.WriteError(stream, "ERR wrong number of arguments for 'ACL SETUSER' command");
            return;
        }

        var username = parts[2];

        var rules = parts.Skip(3).ToList();

        foreach (var rule in rules)
        {
            if (rule.StartsWith(">"))
            {
                var password = rule.Substring(1);
                _store.AddUserPassword(username, HashPassword(password));
            }
            else if (rule == "nopass")
            {
                _store.ClearUserPasswords(username);
            }
        }

        await RespWriter.WriteSimpleString(stream, "OK");
    }

    static string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);

            byte[] hashBytes = sha256.ComputeHash(bytes);

            StringBuilder builder = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}

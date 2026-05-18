using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands;

internal class AuthHandler : ICommandHandler    
{
    private readonly Store _store;
    public AuthHandler(Store store)
    {
        _store = store;
    }
    public CommandsName CommandName => CommandsName.AUTH;
    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        var username = parts[1];
        var password = parts[2];
        var hashedPassword = HashPassword(password);
     
        if (_store.IsValidPassword(username, hashedPassword))
            await RespWriter.WriteSimpleString(stream, "OK");
        else
            await RespWriter.WriteSimpleError(stream, "WRONGPASS invalid username-password pair or user is disabled.");
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

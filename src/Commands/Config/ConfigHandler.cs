using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands.Config;

internal class ConfigHandler : ICommandHandler
{
    private readonly Store _store;
    public ConfigHandler(Store store)
    {
        _store = store;
    }

    public CommandsName CommandName => CommandsName.CONFIG;

    public async Task Handle(NetworkStream stream, List<string> parts, ClientContext context)
    {
        if (parts.Count < 3 || parts[1].ToUpper() != "GET")
        {
            await RespWriter.WriteError(stream, "ERR Unknown subcommand or wrong number of arguments for 'CONFIG'");
            return;
        }

        var paramName = parts[2].ToLower();
        var paramValue = _store.GetConfig(paramName);

        if (paramValue == null)
        {
            await RespWriter.WriteArray(stream, new List<string>());
            return;
        }

        var response = new List<string>
        {
            paramName,
            paramValue
        };

        await RespWriter.WriteArray(stream, response);
    }
}

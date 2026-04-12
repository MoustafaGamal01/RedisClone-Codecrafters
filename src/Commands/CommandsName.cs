using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Commands
{
    public enum CommandsName
    {
        PING,
        ECHO,
        SET,
        GET,
        LRANGE,
        RPUSH,
        LPUSH,
        LLEN,
        LPOP,
        BLPOP,
        TYPE,
        XADD
    }
}

using codecrafters_redis.src.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Core
{
    internal class RedisStream : RedisValue
    {
        public List<Dictionary<string, string>> Entries { get; } = new List<Dictionary<string, string>>();
    }
}

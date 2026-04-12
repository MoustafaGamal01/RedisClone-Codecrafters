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
        public List<(string Id, Dictionary<string, string> Fields)> Entries { get; } = new();
    }
}

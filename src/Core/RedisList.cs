using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Redis
{
    public class RedisList : RedisValue
    {
        public List<string> Items { get; set; } = new();
    }

}

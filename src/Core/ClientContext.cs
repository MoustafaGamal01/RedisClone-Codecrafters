using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Core
{
    public class ClientContext
    {
        public bool IsInTransaction { get; set; } = false;
        public Queue<List<string>> CommandQueue { get; } = new();
    }
}
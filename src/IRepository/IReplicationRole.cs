using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.IRepository;

internal interface IReplicationRole
{
    string Role { get; }
    Task StartAsync(int port);

}

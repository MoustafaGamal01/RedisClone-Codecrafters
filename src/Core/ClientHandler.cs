using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_redis.src.Core
{
    public class ClientHandler
    {
        private readonly CommandHandler _dispatcher;

        public ClientHandler(CommandHandler dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task HandleAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break;
                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var parts = RespParser.Parse(request); 
                await _dispatcher.Dispatch(stream, parts);
            }
        }
    }
}

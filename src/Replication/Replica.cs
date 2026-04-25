using codecrafters_redis.src.Client;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Protocol;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Replication;

public class Replica : IReplicationRole
{
    public string Role => "slave";

    private readonly string _masterHost;
    private readonly int _masterPort;
    private readonly CommandHandler _dispatcher;

    public Replica(string masterHost, int masterPort, CommandHandler dispatcher)
    {
        _masterHost = masterHost;
        _masterPort = masterPort;
        _dispatcher = dispatcher;
    }

    public async Task StartAsync(int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(_masterHost, _masterPort);
        var stream = client.GetStream();

        await SendPing(stream);
        await SendReplConf(stream, port);
        await SendPsync(stream);

        _ = Task.Run(async () =>
        {
            try { await ListenToMaster(stream); }
            catch (Exception ex) { Console.WriteLine($"[Replica] Task crashed: {ex}"); }
        });

    }

    private async Task SendPing(NetworkStream stream)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n"));
        var buffer = new byte[1024];
        await stream.ReadAsync(buffer);
    }

    private async Task SendReplConf(NetworkStream stream, int port)
    {
        var portStr = port.ToString();
        var cmd1 = $"*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n${portStr.Length}\r\n{portStr}\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd1));

        var cmd2 = "*3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd2));

        var buffer = new byte[1024];
        await stream.ReadAsync(buffer);
    }

    private async Task SendPsync(NetworkStream stream)
    {
        var cmd = "*3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd));

        var buffer = new byte[1024];
        await stream.ReadAsync(buffer);

        var rdbBuffer = new byte[4096];
        await stream.ReadAsync(rdbBuffer);
    }


    private async Task ListenToMaster(NetworkStream stream)
    {
        Console.WriteLine("[Replica] ListenToMaster started");
        try
        {
            var buffer = new byte[4096];
            var replicaContext = new ClientContext();
            replicaContext.Replication = this;
            replicaContext.ClientRole["role"] = "slave";
            replicaContext.SuppressResponses = true;

            while (true)
            {
                var bytes = await stream.ReadAsync(buffer);
                if (bytes == 0) break;

                var request = Encoding.UTF8.GetString(buffer, 0, bytes);

                var parts = request.Split('*');

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;

                    var command = "*" + part;
                    var parsed = RespParser.Parse(command);

                    if (parsed.Count == 0) continue;

                    var cmd = parsed[0].ToUpper();
                    if (cmd != "SET" && cmd != "DEL" && cmd != "RPUSH" &&
                        cmd != "LPUSH" && cmd != "XADD" && cmd != "INCR" &&
                        cmd != "HSET" && cmd != "EXPIRE")
                        continue;

                    Console.WriteLine($"[Replica] Applying: {string.Join(" ", parsed)}");
                    await _dispatcher.Dispatch(stream, parsed, replicaContext);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Replica] CRASH: {ex}");
        }
    }
}

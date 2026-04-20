namespace codecrafters_redis.src.Replication;

public class Replica : IReplicationRole
{
    public string Role => "slave";

    private readonly string _masterHost;
    private readonly int _masterPort;

    public Replica(string masterHost, int masterPort)
    {
        _masterHost = masterHost;
        _masterPort = masterPort;
    }

    public async Task StartAsync(int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(_masterHost, _masterPort);
        var stream = client.GetStream();

        await SendPing(stream);
        await SendReplConf(stream, port);
        await SendPsync(stream);

        _ = Task.Run(() => ListenToMaster(stream));
    }

    private async Task SendPing(NetworkStream stream)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n"));

        var buffer = new byte[1024];
        var bytes = await stream.ReadAsync(buffer);
    }

    private async Task SendReplConf(NetworkStream stream, int port)
    {
        var portStr = port.ToString();
        var cmd1 = $"*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n${portStr.Length}\r\n{portStr}\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd1));

        var cmd2 = "*3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd2));

        var buffer = new byte[1024];
        var bytes = await stream.ReadAsync(buffer);
    }

    private async Task SendPsync(NetworkStream stream)
    {
        var cmd = "*3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd));

        var buffer = new byte[1024];
        var bytes = await stream.ReadAsync(buffer);

    }

    private async Task ListenToMaster(NetworkStream stream)
    {
        var buffer = new byte[4096];
        while (true)
        {
            var bytes = await stream.ReadAsync(buffer);
            if (bytes == 0) break; 

            var data = Encoding.UTF8.GetString(buffer, 0, bytes);
        }
    }
}

using codecrafters_redis.src.Client;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Protocol;
using System;
using System.Collections.Generic;
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
        // We do NOT read the response here, because the master will reply with
        // +FULLRESYNC, the RDB file, and propagated commands. 
        // We let ListenToMaster handle all of that to prevent data loss.
    }

    private async Task ListenToMaster(NetworkStream stream)
    {
        var context = new ClientContext { Replication = this };
        context.ClientRole["role"] = "master";
        context.SuppressResponses = true;

        var buffer = new byte[4096];
        var remainder = new List<byte>();

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            for (int i = 0; i < bytesRead; i++)
            {
                remainder.Add(buffer[i]);
            }

            while (remainder.Count > 0)
            {
                if (remainder[0] == (byte)'*')
                {
                    int firstCrLf = IndexOf(remainder, new byte[] { (byte)'\r', (byte)'\n' });
                    if (firstCrLf == -1) break;

                    string countStr = Encoding.UTF8.GetString(remainder.ToArray(), 1, firstCrLf - 1);
                    if (!int.TryParse(countStr, out int numElements))
                    {
                        remainder.RemoveRange(0, firstCrLf + 2);
                        continue;
                    }

                    int currentPos = firstCrLf + 2;
                    var parts = new List<string>();
                    bool hasFullCommand = true;

                    for (int i = 0; i < numElements; i++)
                    {
                        if (currentPos >= remainder.Count) { hasFullCommand = false; break; }
                        if (remainder[currentPos] != (byte)'$') { hasFullCommand = false; break; }

                        int crLf = IndexOf(remainder, new byte[] { (byte)'\r', (byte)'\n' }, currentPos);
                        if (crLf == -1) { hasFullCommand = false; break; }

                        string lenStr = Encoding.UTF8.GetString(remainder.ToArray(), currentPos + 1, crLf - currentPos - 1);
                        int len = int.Parse(lenStr);

                        int dataStart = crLf + 2;
                        if (dataStart + len + 2 > remainder.Count) { hasFullCommand = false; break; }

                        string part = Encoding.UTF8.GetString(remainder.ToArray(), dataStart, len);
                        parts.Add(part);
                        currentPos = dataStart + len + 2;
                    }

                    if (!hasFullCommand) break;

                    remainder.RemoveRange(0, currentPos);
                    await _dispatcher.Dispatch(stream, parts, context);
                }
                else if (remainder[0] == (byte)'$')
                {
                    int crLf = IndexOf(remainder, new byte[] { (byte)'\r', (byte)'\n' });
                    if (crLf == -1) break;

                    string lenStr = Encoding.UTF8.GetString(remainder.ToArray(), 1, crLf - 1);
                    int len = int.Parse(lenStr);
                    int dataStart = crLf + 2;

                    if (dataStart + len > remainder.Count) break;

                    remainder.RemoveRange(0, dataStart + len);
                }
                else
                {
                    int crLf = IndexOf(remainder, new byte[] { (byte)'\r', (byte)'\n' });
                    if (crLf == -1) break;

                    remainder.RemoveRange(0, crLf + 2);
                }
            }
        }
    }

    private int IndexOf(List<byte> list, byte[] pattern, int startIndex = 0)
    {
        for (int i = startIndex; i <= list.Count - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (list[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }
}

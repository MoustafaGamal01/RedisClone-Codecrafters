using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Any, 6379);
listener.Start();
Console.WriteLine("Server is listening on port 6379...");

while (true)
{
    var client = listener.AcceptTcpClient();
    Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);
    
        var stream = client.GetStream();
        var buffer = new byte[1024];

        while (true)
        {
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            stream.Write(Encoding.UTF8.GetBytes("+PONG\r\n"));
        }
    
}
// echo "PING`nP`nP`nP" | .\redis-cli.exe -p 6379


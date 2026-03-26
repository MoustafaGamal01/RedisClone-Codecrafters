using System.Net;
using System.Net.Sockets;
using System.Text;

var listener = new TcpListener(IPAddress.Any, 6379);
listener.Start();

var client = listener.AcceptTcpClient();
var stream = client.GetStream();
var buffer = new byte[1024];

while (true)
{
    var bytesRead = stream.Read(buffer, 0, buffer.Length);
    if (bytesRead == 0) break;

    var response = Encoding.UTF8.GetBytes("+PONG\r\n");
    stream.Write(response, 0, response.Length);
}
using System.Net;
using System.Net.Sockets;
using System.Text;
var listener = new TcpListener(IPAddress.Any, 6379);
listener.Start();
using var client = listener.AcceptTcpClient();
using var stream = client.GetStream();
var response = Encoding.ASCII.GetBytes("+PONG\r\n");
stream.Write(response, 0, response.Length);

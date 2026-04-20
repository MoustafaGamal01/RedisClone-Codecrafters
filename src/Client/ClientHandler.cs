namespace codecrafters_redis.src.Client;

public class ClientHandler
{
    private readonly CommandHandler _dispatcher;
    private readonly string[] _args;
    public ClientHandler(CommandHandler dispatcher, string[] args)
    {
        _dispatcher = dispatcher;
        _args = args;
    }

    public async Task HandleAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        var buffer = new byte[4096];
        var context = new ClientContext();

        if (_args.Length > 3 && _args[2] == "--replicaof") {
            context.ClientRole["role"] = "slave";
            context.slaveCount++;
        }
        else {
            context.ClientRole["role"] = "master";
        }

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var parts = RespParser.Parse(request); 
            await _dispatcher.Dispatch(stream, parts, context);
        }
    }
}

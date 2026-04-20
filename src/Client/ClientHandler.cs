namespace codecrafters_redis.src.Client;

public class ClientHandler
{
    private readonly CommandHandler _dispatcher;
    private readonly string[] _args;
    private readonly ClientContext _context;
    public ClientHandler(CommandHandler dispatcher, string[] args, ClientContext context)
    {
        _dispatcher = dispatcher;
        _args = args;
        _context = context;
    }

    public async Task HandleAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        var buffer = new byte[4096];

        var context = new ClientContext();

        context.Replication = _context.Replication;
        context.ClientRole["role"] = _context.ClientRole.GetValueOrDefault("role", "master");

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

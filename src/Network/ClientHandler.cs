namespace codecrafters_redis.src.Network;

public class ClientHandler
{
    private readonly CommandHandler _dispatcher;
    private readonly string[] _args;
    private readonly ClientContext _context;
    private readonly Store _store;
    public ClientHandler(CommandHandler dispatcher, string[] args, ClientContext context, Store store)
    {
        _dispatcher = dispatcher;
        _args = args;
        _context = context;
        _store = store;
    }

    public async Task HandleAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        var buffer = new byte[4096];

        var context = new ClientContext();

        context.Replication = _context.Replication;
        context.ClientRole["role"] = _context.ClientRole.GetValueOrDefault("role", "master");

        var defaultPasswords = _store.GetUserPasswords("default");
        if (defaultPasswords == null || defaultPasswords.Count == 0)
        {
            context.AuthenticatedUser = "default";
        }
        else
        {
            context.AuthenticatedUser = null;
        }

        try
        {
            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break;
                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var parts = RespParser.Parse(request);
                try
                {
                    await _dispatcher.Dispatch(stream, parts, context);
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Internal Error] Command execution failed: {ex}");
                    try
                    {
                        await RespWriter.WriteError(stream, "internal server error");
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
    }
}

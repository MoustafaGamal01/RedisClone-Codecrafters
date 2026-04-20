namespace codecrafters_redis.src.IRepository;

public interface IReplicationRole
{
    string Role { get; }
    Task StartAsync(int port);

}

namespace codecrafters_redis.src.Common.Interfaces;

public interface IReplicationRole
{
    string Role { get; }
    Task StartAsync(int port);

}

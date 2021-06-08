using StackExchange.Redis;

namespace LitRedis.Core.Interfaces
{
    public interface ILitRedisConnection
    {
        void ForceReconnect();

        ConnectionMultiplexer GetConnectionMultiplexer();
    }
}

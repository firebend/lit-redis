using System.Threading.Tasks;
using StackExchange.Redis;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisConnection
{
    void ForceReconnect();

    Task<ConnectionMultiplexer> GetConnectionMultiplexer();
}

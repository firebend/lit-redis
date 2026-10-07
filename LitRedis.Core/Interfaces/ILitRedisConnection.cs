using System.Threading.Tasks;
using StackExchange.Redis;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisConnection
{
    public void ForceReconnect();

    public Task<ConnectionMultiplexer> GetConnectionMultiplexer();
}

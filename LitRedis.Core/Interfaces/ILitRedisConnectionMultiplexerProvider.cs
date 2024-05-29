using System.Threading.Tasks;
using StackExchange.Redis;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisConnectionMultiplexerProvider
{
    Task<IConnectionMultiplexer> GetConnectionMultiplexerAsync();
}

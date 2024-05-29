using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LitRedis.Core.Implementations;

public class LitRedisConnectionMultiplexerProvider : ILitRedisConnectionMultiplexerProvider
{
    private readonly RedisCacheOptions _redisCacheOptions;

    public LitRedisConnectionMultiplexerProvider(IOptions<RedisCacheOptions> options)
    {
        _redisCacheOptions = options.Value;
    }

    public async Task<IConnectionMultiplexer> GetConnectionMultiplexerAsync()
    {
        if (_redisCacheOptions.ConnectionMultiplexerFactory != null)
        {
            return await _redisCacheOptions.ConnectionMultiplexerFactory();
        }

        if (_redisCacheOptions.ConfigurationOptions != null)
        {
            return await ConnectionMultiplexer.ConnectAsync(_redisCacheOptions.ConfigurationOptions);
        }

        if (!string.IsNullOrWhiteSpace(_redisCacheOptions.Configuration))
        {
            return await ConnectionMultiplexer.ConnectAsync(_redisCacheOptions.Configuration);
        }

        return null;
    }
}

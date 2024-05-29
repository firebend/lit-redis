using System;
using System.Threading.Tasks;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LitRedis.Core.Builders;

public interface ILitRedisConnectionMultiplexerProvider
{
    Task<IConnectionMultiplexer> GetConnectionMultiplexerAsync();
}

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

public class LitRedisServiceCollectionBuilder
{
    public string ConnectionString { get; }
    public IServiceCollection ServiceCollection { get; }

    public LitRedisServiceCollectionBuilder(string connectionString, IServiceCollection serviceCollection)
    {
        ConnectionString = connectionString;
        ServiceCollection = serviceCollection;
        ServiceCollection.TryAddScoped<ILitRedisConnectionMultiplexerProvider, LitRedisConnectionMultiplexerProvider>();
    }

    public LitRedisServiceCollectionBuilder WithCaching(
        Action<RedisCacheOptions> configureRedis = null,
        Action<HybridCacheOptions> configureHybridCache = null)
    {
        ServiceCollection.TryAddScoped<ILitRedisCacheStore, LitRedisCacheStore>();
        ServiceCollection.AddMemoryCache();

        ServiceCollection.AddStackExchangeRedisCache(opt =>
        {
            opt.Configuration = ConnectionString;
            configureRedis?.Invoke(opt);
        });

        ServiceCollection.AddHybridCache(opt =>
        {
            configureHybridCache?.Invoke(opt);
        });

        return this;
    }

    public LitRedisServiceCollectionBuilder WithLocking()
    {
        ServiceCollection.TryAddScoped<ILitRedisDistributedLock, LitRedisDistributedLock>();
        ServiceCollection.TryAddScoped<ILitRedisDistributedLockService, LitRedisDistributedLockService>();
        return this;
    }
}

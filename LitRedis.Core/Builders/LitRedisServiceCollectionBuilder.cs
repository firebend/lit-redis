using System;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace LitRedis.Core.Builders;

public class LitRedisServiceCollectionBuilder
{
    private bool _cacheAdded;
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
            opt.ConfigurationOptions = ConfigurationOptions.Parse(ConnectionString, true);
            configureRedis?.Invoke(opt);
        });

        ServiceCollection.AddHybridCache(opt =>
        {
            configureHybridCache?.Invoke(opt);
        });

        _cacheAdded = true;

        return this;
    }

    public LitRedisServiceCollectionBuilder WithLocking()
    {
        if (_cacheAdded is false)
        {
            throw new Exception("Please add caching before adding locking");
        }

        ServiceCollection.TryAddScoped<ILitRedisDistributedLock, LitRedisDistributedLock>();
        ServiceCollection.TryAddScoped<ILitRedisDistributedLockService, LitRedisDistributedLockService>();
        return this;
    }
}

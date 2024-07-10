using System;
using System.Text.Json;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LitRedis.Core.Builders;

public class LitRedisServiceCollectionBuilder
{
    public IServiceCollection ServiceCollection { get; }
    private readonly LitRedisOptions _litRedisOptions = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public LitRedisServiceCollectionBuilder(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;

        serviceCollection.TryAddSingleton(_litRedisOptions);
        serviceCollection.TryAddSingleton<ILitRedisJsonSerializer, LitRedisSystemTextJsonSerializer>();
        serviceCollection.TryAddSingleton<ILitRedisSystemTextJsonOptionsProvider>(new DefaultLitRedisSystemTextJsonOptionsProvider(_jsonOptions));
        serviceCollection.TryAddSingleton<ILitRedisConnection, LitLitRedisConnection>();
        serviceCollection.TryAddScoped<ILitRedisConnectionService, LitRedisConnectionService>();
    }

    public LitRedisServiceCollectionBuilder WithCaching()
    {
        ServiceCollection.TryAddScoped<ILitRedisCacheStore, LitRedisCacheStore>();
        ServiceCollection.AddMemoryCache();
        return this;
    }

    public LitRedisServiceCollectionBuilder WithLocking()
    {
        ServiceCollection.TryAddScoped<ILitRedisDistributedLock, LitRedisDistributedLock>();
        ServiceCollection.TryAddScoped<ILitRedisDistributedLockService, LitRedisDistributedLockService>();
        return this;
    }


    public LitRedisServiceCollectionBuilder WithConnectionString(string connString)
        => WithLitRedisOptions(o => o.ConnectionString = connString);

    public LitRedisServiceCollectionBuilder WithLitRedisOptions(Action<LitRedisOptions> configure)
    {
        configure(_litRedisOptions);
        return this;
    }

    public LitRedisServiceCollectionBuilder WithJsonSerializer<T>() where T : class, ILitRedisJsonSerializer
    {
        ServiceCollection.RemoveAll<ILitRedisJsonSerializer>();
        ServiceCollection.TryAddSingleton<ILitRedisJsonSerializer, T>();
        return this;
    }

    public LitRedisServiceCollectionBuilder WithJsonOptions(Action<JsonSerializerOptions> configure)
    {
        configure(_jsonOptions);
        return this;
    }
}

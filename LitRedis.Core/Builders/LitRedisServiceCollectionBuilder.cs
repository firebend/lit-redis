using System;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LitRedis.Core.Builders
{
    //todo: make it
    //todo: test
    public class LitRedisServiceCollectionBuilder
    {
        public IServiceCollection ServiceCollection { get; }
        private readonly LitRedisOptions _litRedisOptions = new();

        public LitRedisServiceCollectionBuilder(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
            serviceCollection.TryAddSingleton(_litRedisOptions);
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
    }
}

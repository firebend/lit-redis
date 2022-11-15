using System;
using LitRedis.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace LitRedis.Core;

//todo: test
public static class ServiceProviderExtensions
{
    public static IServiceCollection AddLitRedis(this IServiceCollection serviceCollection,
        Action<LitRedisServiceCollectionBuilder> configure)
    {
        var builder = new LitRedisServiceCollectionBuilder(serviceCollection);
        configure(builder);
        return serviceCollection;
    }
}

using System;
using LitRedis.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace LitRedis.Core;

//todo: test
public static class ServiceProviderExtensions
{
    public static IServiceCollection AddLitRedis(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<LitRedisServiceCollectionBuilder> configure)
    {
        var builder = new LitRedisServiceCollectionBuilder(connectionString, serviceCollection);
        configure(builder);
        return serviceCollection;
    }
}

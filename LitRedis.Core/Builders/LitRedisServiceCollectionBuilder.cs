using Microsoft.Extensions.DependencyInjection;

namespace LitRedis.Core.Builders
{
    //todo: make it
    //todo: test
    public class LitRedisServiceCollectionBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        public LitRedisServiceCollectionBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }
    }
}

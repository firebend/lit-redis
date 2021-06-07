using System;
using System.IO;
using System.Threading.Tasks;
using LitRedis.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LitRedis.Sample
{
    internal static class Program
    {
        private static Task Main(string[] args) =>
            CreateHostBuilder(args).Build().RunAsync();

        private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddEnvironmentVariables();
                configHost.AddCommandLine(args);
            })
            .ConfigureServices((_, services) =>
            {
                services
                    .AddLitRedis(redis => redis.WithCaching().WithLocking().WithConnectionString("localhost:6379,defaultDatabase=0"))
                    .AddHostedService<SampleHostedService>()
                    .AddLogging(o => o.AddSimpleConsole(c => c.TimestampFormat = "[yyy-MM-dd HH:mm:ss] "));
            });
    }
}

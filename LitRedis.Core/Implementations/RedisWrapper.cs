using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LitRedis.Core.Implementations
{
    public class RedisWrapper : IRedisWrapper
    {
        protected ILogger Logger { get; }

        protected string Server { get; }

        protected static ConnectionMultiplexer Connection => LitRedisConnection.Connection;

        public RedisWrapper(IOptions<LitRedisOptions> options, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<RedisWrapper>();

            var connectionString = options?.Value?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Redis connection string is null or empty");
            }

            Server = connectionString.Split(',').FirstOrDefault();

            LitRedisConnection.InitializeConnectionString(connectionString);
        }

        public async Task<T> UseRedisAsync<T>(Func<ConnectionMultiplexer, CancellationToken, Task<T>> func, CancellationToken cancellationToken = default)
        {
            try
            {
                return await func(Connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogWarning("Error using redis. Exception: {Exception}", e);

                DoForceReconnect(e);
            }

            return default;
        }

        public async Task UseRedisAsync(Func<ConnectionMultiplexer, CancellationToken, Task> func, CancellationToken cancellationToken = default)
        {
            try
            {
                await func(Connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogWarning("Error using redis. Exception: {Exception}", e);

                DoForceReconnect(e);
            }
        }

        public Task<T> UseDbAsync<T>(Func<IDatabase, CancellationToken, Task<T>> fn, CancellationToken cancellationToken = default)
            => UseRedisAsync((multiplexer, ct) => fn(multiplexer.GetDatabase(), ct), cancellationToken);

        public Task<T> UseServerAsync<T>(Func<IServer, CancellationToken, Task<T>> fn, CancellationToken cancellationToken = default)
            => UseRedisAsync((multiplexer, ct) => fn(multiplexer.GetServer(Server), ct), cancellationToken);

        public Task UseServerAsync(Func<IServer, CancellationToken, Task> fn, CancellationToken cancellationToken = default)
            => UseRedisAsync((multiplexer, ct) => fn(multiplexer.GetServer(Server), ct), cancellationToken);

        protected void DoForceReconnect(Exception ex)
        {
            var shouldReconnect = ex is RedisConnectionException or SocketException;

            if (!shouldReconnect)
            {
                return;
            }

            try
            {
                LitRedisConnection.ForceReconnect();
            }
            catch (Exception e)
            {
                Logger.LogWarning("Could not force reconnect redis. Exception: {Exception}", e);
            }
        }
    }
}

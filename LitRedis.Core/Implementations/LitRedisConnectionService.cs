using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LitRedis.Core.Implementations
{
    public class LitRedisConnectionService : ILitRedisConnectionService
    {
        private readonly ILitRedisConnection _litRedisConnection;
        private ILogger Logger { get; }

        private string Server { get; }

        private ConnectionMultiplexer Connection => _litRedisConnection.GetConnectionMultiplexer();

        public LitRedisConnectionService(
            ILitRedisConnection litRedisConnection,
            LitRedisOptions options,
            ILoggerFactory loggerFactory)
        {
            _litRedisConnection = litRedisConnection;
            Logger = loggerFactory.CreateLogger<LitRedisConnectionService>();

            var connectionString = options?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Redis connection string is null or empty");
            }

            Server = connectionString.Split(',').FirstOrDefault();
        }

        public async Task<T> UseRedisAsync<T>(Func<ConnectionMultiplexer, CancellationToken, Task<T>> func, CancellationToken cancellationToken)
        {
            try
            {
                return await func(Connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogWarning("Error using redis. Exception: {Exception}", e);

                DoForceReconnect(e);

                throw;
            }
        }

        public async Task UseRedisAsync(Func<ConnectionMultiplexer, CancellationToken, Task> func, CancellationToken cancellationToken)
        {
            try
            {
                await func(Connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogWarning("Error using redis. Exception: {Exception}", e);

                DoForceReconnect(e);

                throw;
            }
        }

        public Task<T> UseDbAsync<T>(Func<IDatabase, CancellationToken, Task<T>> fn, CancellationToken cancellationToken)
            => UseRedisAsync((multiplexer, ct) => fn(multiplexer.GetDatabase(), ct), cancellationToken);

        public Task<T> UseServerAsync<T>(Func<IServer, CancellationToken, Task<T>> fn, CancellationToken cancellationToken)
            => UseRedisAsync((multiplexer, ct) => fn(multiplexer.GetServer(Server), ct), cancellationToken);

        public Task UseServerAsync(Func<IServer, CancellationToken, Task> fn, CancellationToken cancellationToken)
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
                _litRedisConnection.ForceReconnect();
            }
            catch (Exception e)
            {
                Logger.LogWarning("Could not force reconnect redis. Exception: {Exception}", e);
            }
        }
    }
}

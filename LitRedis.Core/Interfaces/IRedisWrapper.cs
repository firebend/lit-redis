using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LitRedis.Core.Interfaces
{
    public interface IRedisWrapper
    {
        Task<T> UseRedisAsync<T>(Func<ConnectionMultiplexer, CancellationToken, Task<T>> func, CancellationToken cancellationToken = default);

        Task UseRedisAsync(Func<ConnectionMultiplexer, CancellationToken, Task> func, CancellationToken cancellationToken = default);

        Task<T> UseDbAsync<T>(Func<IDatabase, CancellationToken, Task<T>> fn, CancellationToken cancellationToken = default);

        Task<T> UseServerAsync<T>(Func<IServer, CancellationToken, Task<T>> fn, CancellationToken cancellationToken = default);

        Task UseServerAsync(Func<IServer, CancellationToken, Task> fn, CancellationToken cancellationToken = default);
    }
}

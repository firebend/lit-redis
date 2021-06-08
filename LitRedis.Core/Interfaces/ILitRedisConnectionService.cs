using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LitRedis.Core.Interfaces
{
    public interface ILitRedisConnectionService
    {
        Task<T> UseDbAsync<T>(Func<IDatabase, CancellationToken, Task<T>> fn, CancellationToken cancellationToken);

        Task<T> UseServerAsync<T>(Func<IServer, CancellationToken, Task<T>> fn, CancellationToken cancellationToken);

        Task UseServerAsync(Func<IServer, CancellationToken, Task> fn, CancellationToken cancellationToken);
    }
}

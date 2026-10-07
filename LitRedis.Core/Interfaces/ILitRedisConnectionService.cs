using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisConnectionService
{
    public Task<T> UseDbAsync<T>(Func<IDatabase, CancellationToken, Task<T>> fn, CancellationToken cancellationToken);

    public Task<T> UseServerAsync<T>(Func<IServer, CancellationToken, Task<T>> fn, CancellationToken cancellationToken);

    public Task UseServerAsync(Func<IServer, CancellationToken, Task> fn, CancellationToken cancellationToken);
}

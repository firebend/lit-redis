using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisCacheStore
{
    public Task PutAsync<T>(string key, T model, TimeSpan? expiry, CancellationToken cancellationToken);

    public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken);

    public Task<string> GetAsync(string key, CancellationToken cancellationToken);

    public Task ClearAsync(string key, CancellationToken cancellationToken);

    public Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken);

    public Task ClearAllAsync(CancellationToken cancellationToken);

    public Task SetExpiryAsync(string key, TimeSpan span, CancellationToken cancellationToken);
}

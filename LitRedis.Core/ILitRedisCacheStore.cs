using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LitRedis.Core
{
    public interface ILitRedisCacheStore
    {
        Task PutAsync<T>(string key, T model, TimeSpan? expiry, CancellationToken cancellationToken = default);

        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task<string> GetAsync(string key, CancellationToken cancellationToken = default);

        Task ClearAsync(string key, CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken = default);

        Task ClearAllAsync(CancellationToken cancellationToken = default);

        Task SetExpiryAsync(string key, TimeSpan span, CancellationToken cancellationToken = default);
    }
}

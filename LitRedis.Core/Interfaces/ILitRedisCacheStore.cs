using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LitRedis.Core.Interfaces
{
    public interface ILitRedisCacheStore
    {
        Task PutAsync<T>(string key, T model, TimeSpan? expiry, CancellationToken cancellationToken);

        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken);

        Task<string> GetAsync(string key, CancellationToken cancellationToken);

        Task ClearAsync(string key, CancellationToken cancellationToken);

        Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken);

        Task ClearAllAsync(CancellationToken cancellationToken);

        Task SetExpiryAsync(string key, TimeSpan span, CancellationToken cancellationToken);
    }
}

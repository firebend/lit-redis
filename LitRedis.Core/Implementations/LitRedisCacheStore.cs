using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace LitRedis.Core.Implementations;

public class LitRedisCacheStore : ILitRedisCacheStore
{
    private readonly HybridCache _hybridCache;
    private readonly ILitRedisConnectionMultiplexerProvider _connectionMultiplexerProvider;

    public LitRedisCacheStore(HybridCache hybridCache, ILitRedisConnectionMultiplexerProvider connectionMultiplexerProvider)
    {
        _hybridCache = hybridCache;
        _connectionMultiplexerProvider = connectionMultiplexerProvider;
    }

    private static void KeyGuard(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Redis Key cannot be null or empty!");
        }
    }

    /// <inheritdoc />
    public async Task PutAsync<T>(string key, T model, TimeSpan? expiry, CancellationToken cancellationToken)
    {
        KeyGuard(key);

        if (model == null || model.Equals(default(T)))
        {
            await ClearAsync(key, cancellationToken);

            return;
        }

        await _hybridCache.SetAsync(key,
            model,
            new () { Expiration = expiry, LocalCacheExpiration = expiry },
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        KeyGuard(key);

        var result = await _hybridCache.GetOrCreateAsync<T>(key, _ => default, null, null, cancellationToken);
        return result;
    }


    /// <inheritdoc />
    public async Task ClearAsync(string key, CancellationToken cancellationToken)
    {
        KeyGuard(key);

        await _hybridCache.RemoveKeyAsync(key, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken)
    {
        var conn = await _connectionMultiplexerProvider.GetConnectionMultiplexerAsync();
        var server = conn.GetServers().First();
        var keys = server.Keys().Select(x => x.ToString());
        return keys;
    }

    /// <inheritdoc />
    public async Task ClearAllAsync(CancellationToken cancellationToken)
    {
        var keys = await GetAllKeys(cancellationToken);

        await _hybridCache.RemoveKeysAsync(keys, cancellationToken);
    }

    public async Task SetExpiryAsync(string key, TimeSpan span, CancellationToken cancellationToken)
    {
        var found = await GetAsync<object>(key, cancellationToken);

        if (found is not null)
        {
            await _hybridCache.SetAsync(key,
                found,
                new(){ Expiration = span, LocalCacheExpiration = span},
                null,
                cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace LitRedis.Core.Implementations;

public class LitRedisCacheStore : ILitRedisCacheStore
{
    private readonly IMemoryCache _cache;
    private readonly ILitRedisConnectionService _litRedisConnectionService;

    public LitRedisCacheStore(IMemoryCache cache, ILitRedisConnectionService litRedisConnectionService)
    {
        _cache = cache;
        _litRedisConnectionService = litRedisConnectionService;
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
        cancellationToken.ThrowIfCancellationRequested();

        KeyGuard(key);

        if (model == null || model.Equals(default(T)))
        {
            await ClearAsync(key, cancellationToken);

            return;
        }

        var str = JsonSerializer.Serialize(model);

        await _litRedisConnectionService.UseDbAsync((db, _) => db.StringSetAsync(key, str, expiry), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        KeyGuard(key);

        var str = await GetAsync(key, cancellationToken);

        return string.IsNullOrWhiteSpace(str) ? default : JsonSerializer.Deserialize<T>(str);
    }

    /// <inheritdoc />
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        KeyGuard(key);

        if (_cache.TryGetValue<string>(key, out var value))
        {
            return value;
        }

        if (_cache.TryGetValue<string>(key, out var value2))
        {
            return value2;
        }

        var str = await _litRedisConnectionService.UseDbAsync(
            (db, _) => db.StringGetAsync(key), cancellationToken);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .AddExpirationToken(
                new CancellationChangeToken(
                    new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token));

        _cache.Set(key, str, cacheEntryOptions);

        return str;
    }

    /// <inheritdoc />
    public async Task ClearAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        KeyGuard(key);

        _cache.Remove(key);

        await _litRedisConnectionService.UseDbAsync((db, _) => db.KeyDeleteAsync(key), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken) =>
        _litRedisConnectionService.UseServerAsync((server, _) => Task.FromResult(server.Keys().Select(x => x.ToString())), cancellationToken);

    /// <inheritdoc />
    public Task ClearAllAsync(CancellationToken cancellationToken) =>
        _litRedisConnectionService.UseServerAsync((server, _) => server.FlushAllDatabasesAsync(), cancellationToken);

    public Task SetExpiryAsync(string key, TimeSpan span, CancellationToken cancellationToken) =>
        _litRedisConnectionService.UseDbAsync((db, _) => db.KeyExpireAsync(key, span), cancellationToken);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;

namespace LitRedis.Tests.Mocks;

public class HybridCacheMock : HybridCache
{
    public Dictionary<string, object> Dictionary { get; } = new();

    public override async ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions options = null,
        IReadOnlyCollection<string> tags = null, CancellationToken token = new())
    {
        if(Dictionary.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        var created = await factory(state, token);
        Dictionary.TryAdd(key, created);

        return created;
    }

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions options = null,
        IReadOnlyCollection<string> tags = null,
        CancellationToken token = new())
    {
        Dictionary[key] = value;
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = new())
    {
        Dictionary.Remove(key);
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = new())
        => ValueTask.CompletedTask;
}

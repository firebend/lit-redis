using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using StackExchange.Redis;

namespace LitRedis.Core.Implementations;

public class LitRedisDistributedLock : ILitRedisDistributedLock
{
    private readonly ILitRedisConnectionMultiplexerProvider _connectionMultiplexer;

    public LitRedisDistributedLock(ILitRedisConnectionMultiplexerProvider connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    private async Task<IDatabase> GetDatabaseAsync()
    {
        var conn = await _connectionMultiplexer.GetConnectionMultiplexerAsync();

        return conn.GetDatabase();
    }

    private static void KeyGuard(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Redis Key cannot be null or empty!");
        }
    }

    public async Task<bool> TakeLockAsync(string key, string token, TimeSpan expiryTime, CancellationToken cancellationToken)
    {
        KeyGuard(key);

        var db = await GetDatabaseAsync();
        return await db.LockTakeAsync(key, token, expiryTime);
    }

    public async Task<bool> ReleaseLockAsync(string key, string token, CancellationToken cancellationToken)
    {
        KeyGuard(key);

        var db = await GetDatabaseAsync();
        return await db.LockReleaseAsync(key, token);
    }

    public async Task<bool> ExtendLockAsync(string key, string token, TimeSpan expiryTime, CancellationToken cancellationToken)
    {
        KeyGuard(key);

        var db = await GetDatabaseAsync();
        return await db.LockExtendAsync(key, token, expiryTime);
    }
}

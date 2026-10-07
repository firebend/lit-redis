using System;
using System.Threading;
using System.Threading.Tasks;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisDistributedLock
{
    public Task<bool> TakeLockAsync(
        string key,
        string token,
        TimeSpan expiryTime,
        CancellationToken cancellationToken);

    public Task<bool> ReleaseLockAsync(
        string key,
        string token,
        CancellationToken cancellationToken);

    public Task<bool> ExtendLockAsync(
        string key,
        string token,
        TimeSpan expiryTime,
        CancellationToken cancellationToken);
}

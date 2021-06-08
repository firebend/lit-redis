using System;
using System.Threading;
using System.Threading.Tasks;

namespace LitRedis.Core.Interfaces
{
    public interface ILitRedisDistributedLock
    {
        Task<bool> TakeLockAsync(
            string key,
            string token,
            TimeSpan expiryTime,
            CancellationToken cancellationToken);

        Task<bool> ReleaseLockAsync(
            string key,
            string token,
            CancellationToken cancellationToken);

        Task<bool> ExtendLockAsync(
            string key,
            string token,
            TimeSpan expiryTime,
            CancellationToken cancellationToken);
    }
}

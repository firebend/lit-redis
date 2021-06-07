using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;

namespace LitRedis.Core.Implementations
{
    public class RedisLitRedisDistributedLock : ILitRedisDistributedLock
    {
        private readonly IRedisWrapper _redisWrapper;

        public RedisLitRedisDistributedLock(IRedisWrapper redisWrapper)
        {
            _redisWrapper = redisWrapper;
        }

        private static void KeyGuard(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key), "Redis Key cannot be null or empty!");
            }
        }

        public Task<bool> TakeLockAsync(string key, string token, TimeSpan expiryTime, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            KeyGuard(key);

            return _redisWrapper.UseDbAsync((db, _) => db.LockTakeAsync(key, token, expiryTime), cancellationToken);
        }

        public Task<bool> ReleaseLockAsync(string key, string token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            KeyGuard(key);

            return _redisWrapper.UseDbAsync((db, _) => db.LockReleaseAsync(key, token), cancellationToken);
        }

        public Task<bool> ExtendLockAsync(string key, string token, TimeSpan expiryTime, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            KeyGuard(key);

            return _redisWrapper.UseDbAsync((db, _) => db.LockExtendAsync(key, token, expiryTime), cancellationToken);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Models;

namespace LitRedis.Core.Interfaces
{
    public interface ILitRedisDistributedLockService
    {
        Task<LitRedisDistributedLockModel> AcquireLockNoWaitAsync(string key, CancellationToken cancellationToken = default);

        Task<LitRedisDistributedLockModel> AcquireLockAsync(string key, TimeSpan? acquireLockTimeout = null, CancellationToken cancellationToken = default);
    }
}

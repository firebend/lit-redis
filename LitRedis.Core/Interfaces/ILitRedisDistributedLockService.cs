using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Models;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisDistributedLockService
{
    Task<LitRedisDistributedLockModel> AcquireLockAsync(RequestLockModel model, CancellationToken cancellationToken);
}

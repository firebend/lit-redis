namespace LitRedis.Core.Models;

public enum LitRedisLockStatus
{
    NotAcquired = 0,
    Acquired = 1,
    Lost = 2,
}

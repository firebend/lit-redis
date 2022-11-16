using System;
using System.Threading.Tasks;
using LitRedis.Core.Exceptions;

namespace LitRedis.Core.Models;

public class LitRedisDistributedLockModel : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    private Func<Task> ReleaseAction { get; set; }

    public bool Succeeded { get; set; }

    public bool WasCancelled { get; set; }

    public TimeSpan? WaitTime { get; set; }

    public LitRedisDistributedLockModel(bool succeeded, Func<Task> releaseAction, bool wasCancelled, TimeSpan? waitTime)
    {
        Succeeded = succeeded;
        ReleaseAction = releaseAction;
        WasCancelled = wasCancelled;
        WaitTime = waitTime;
    }

    public static LitRedisDistributedLockModel Success(Func<Task> releaseAction = null)
        => new(true, releaseAction, false, null);

    public static LitRedisDistributedLockModel Failure(bool wasCancelled = false, TimeSpan? waitTime = null)
        => new(false, null, wasCancelled, waitTime);

    private async Task ReleaseAsync()
    {
        if (ReleaseAction != null)
        {
            await ReleaseAction();
        }

        ReleaseAction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await ReleaseAsync();

        _disposed = true;
    }

    public void ThrowIfFailedToAcquire()
    {
        if (!Succeeded)
        {
            throw new AcquireLockFailedException(WaitTime.GetValueOrDefault());
        }
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}

using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Exceptions;

namespace LitRedis.Core.Models;

public class LitRedisDistributedLockModel : IDisposable, IAsyncDisposable
{
    private int _disposed;
    private int _lostFlag;
    private CancellationTokenSource _lockLostCts;

    private Func<Task> ReleaseAction { get; set; }

    public bool Succeeded { get; set; }

    public bool WasCancelled { get; set; }

    public TimeSpan? WaitTime { get; set; }

    /// <summary>
    /// Gets the current status of the distributed lock.
    /// </summary>
    public LitRedisLockStatus Status { get; internal set; } = LitRedisLockStatus.NotAcquired;

    /// <summary>
    /// Returns true if the lock has been lost. Thread-safe.
    /// </summary>
    public bool IsLost => Volatile.Read(ref _lostFlag) == 1;

    /// <summary>
    /// Cancelled if the background renewal loop fails to extend the lock, indicating the lock may have been lost.
    /// </summary>
    public CancellationToken LockLostToken => _lockLostCts?.Token ?? CancellationToken.None;

    /// <summary>
    /// Gets the key that was locked.
    /// </summary>
    public string Key { get; }

    public LitRedisDistributedLockModel(bool succeeded, Func<Task> releaseAction, bool wasCancelled, TimeSpan? waitTime, CancellationTokenSource lockLostCts = null, string key = null)
    {
        Succeeded = succeeded;
        ReleaseAction = releaseAction;
        WasCancelled = wasCancelled;
        WaitTime = waitTime;
        _lockLostCts = lockLostCts;
        Key = key;
    }

    public static LitRedisDistributedLockModel Success(Func<Task> releaseAction = null, CancellationTokenSource lockLostCts = null, string key = null)
        => new(true, releaseAction, false, null, lockLostCts, key)
        {
            Status = LitRedisLockStatus.Acquired,
        };

    public static LitRedisDistributedLockModel Failure(bool wasCancelled = false, TimeSpan? waitTime = null)
        => new(false, null, wasCancelled, waitTime) { Status = LitRedisLockStatus.NotAcquired };

    /// <summary>
    /// Marks the lock as lost and signals <see cref="LockLostToken"/>.
    /// </summary>
    internal void MarkLost()
    {
        if (Interlocked.CompareExchange(ref _lostFlag, 1, 0) != 0)
        {
            return;
        }

        Status = LitRedisLockStatus.Lost;

        // Signal the cancellation token to notify callers that the lock was lost.
        // Do not null out the field here so callers can still observe the cancelled
        // token via the LockLostToken property. Disposal will occur in DisposeAsync.
        var cts = Volatile.Read(ref _lockLostCts);
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // CTS was already disposed; nothing to signal.
        }
    }

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
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        await ReleaseAsync();

        var cts = Interlocked.Exchange(ref _lockLostCts, null);
        cts?.Dispose();
    }

    public void ThrowIfFailedToAcquire()
    {
        if (!Succeeded)
        {
            throw new AcquireLockFailedException(Key, WaitTime.GetValueOrDefault());
        }
    }

    /// <summary>
    /// Throws a <see cref="LockLostException"/> if the background renewal loop has reported that the lock was lost.
    /// </summary>
    public void ThrowOnLockLost()
    {
        if (IsLost)
        {
            throw new LockLostException(Key);
        }
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}

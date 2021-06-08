using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Logging;

namespace LitRedis.Core.Implementations
{
    public class LitRedisDistributedLockService : ILitRedisDistributedLockService
    {
        private readonly ILitRedisDistributedLock _litRedisDistributedLock;
        private readonly ILogger _logger;

        public LitRedisDistributedLockService(ILitRedisDistributedLock litRedisDistributedLock,
            ILoggerFactory loggerFactory)
        {
            _litRedisDistributedLock = litRedisDistributedLock;
            _logger = loggerFactory.CreateLogger<LitRedisDistributedLockService>();
        }

        public async Task<LitRedisDistributedLockModel> AcquireLockAsync(
            RequestLockModel requestLockModel,
            CancellationToken cancellationToken)
        {
            var token = Guid.NewGuid().ToString();
            var waitForever = requestLockModel.WaitTimeout == null;
            var startTime = DateTime.Now;
            var stopTimeTicks = waitForever ? 0 : startTime.Add(requestLockModel.WaitTimeout.GetValueOrDefault()).Ticks;
            var rnd = new Random();
            var lockKey = MakeKey(requestLockModel.Key);

            do
            {

                if (await _litRedisDistributedLock.TakeLockAsync(lockKey, token, requestLockModel.LockIncrease, cancellationToken))
                {
                    var stopped = false;

                    var _ = Task.Run(async () =>
                    {
                        while (!stopped && !cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                await _litRedisDistributedLock.ExtendLockAsync(lockKey, token, requestLockModel.LockIncrease, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Extended Distributed Lock Error");
                            }

                            try
                            {
                                await Task.Delay(requestLockModel.RenewLockInterval, cancellationToken);
                            }
                            catch (TaskCanceledException) { }
                        }
                    }, cancellationToken);

                    return LitRedisDistributedLockModel.Success(
                        async () =>
                        {
                            try
                            {
                                //stop the keep alive thread and release the lock
                                stopped = true;
                                await _litRedisDistributedLock.ReleaseLockAsync(lockKey, token, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error releasing lock");
                            }
                        }
                    );
                }

                try
                {
                    //if we didn't get the lock, wait 100+-50 milliseconds and try again
                    var diff = rnd.Next(-50, 50);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 + diff), cancellationToken);
                }
                catch (TaskCanceledException) { }
            }
            while ((waitForever || DateTime.Now.Ticks < stopTimeTicks) && !cancellationToken.IsCancellationRequested);

            return LitRedisDistributedLockModel.Failure(cancellationToken.IsCancellationRequested, DateTime.Now - startTime);
        }

        private static string MakeKey(string key) => $"LIT_LOCK_{key}";
    }
}

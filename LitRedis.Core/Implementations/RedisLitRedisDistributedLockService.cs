using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Logging;

namespace LitRedis.Core.Implementations
{
    public class RedisLitRedisDistributedLockService : ILitRedisDistributedLockService
    {
        private readonly ILitRedisDistributedLock _litRedisDistributedLock;
        private readonly ILogger _logger;

        public RedisLitRedisDistributedLockService(ILitRedisDistributedLock litRedisDistributedLock,
            ILoggerFactory loggerFactory)
        {
            _litRedisDistributedLock = litRedisDistributedLock;
            _logger = loggerFactory.CreateLogger<RedisLitRedisDistributedLockService>();
        }

        public Task<LitRedisDistributedLockModel> AcquireLockNoWaitAsync(string key,
            CancellationToken cancellationToken = default)
            => AcquireLockAsync(key, TimeSpan.Zero, cancellationToken);

        public async Task<LitRedisDistributedLockModel> AcquireLockAsync(string key,
            TimeSpan? acquireLockTimeout = null,
            CancellationToken cancellationToken = default)
        {
            var token = Guid.NewGuid().ToString();
            var waitForever = acquireLockTimeout == null;
            var startTime = DateTime.Now;
            var stopTimeTicks = waitForever ? 0 : startTime.Add(acquireLockTimeout.GetValueOrDefault()).Ticks;
            var rnd = new Random();

            do
            {
                var lockKey = MakeKey(key);
                //acquire the lock with an initial duration of 1 minute
                if (await _litRedisDistributedLock.TakeLockAsync(lockKey, token, TimeSpan.FromMinutes(1), cancellationToken))
                {
                    var stopped = false;

                    var keepAliveThread = Task.Run(async () =>
                    {
                        while (!stopped && !cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                //every 30 seconds while the task is still running, extend the lock an additional 1 minute
                                await _litRedisDistributedLock.ExtendLockAsync(lockKey, token, TimeSpan.FromMinutes(1), cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Extended Distributed Lock Error");
                            }

                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
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
                                await _litRedisDistributedLock.ReleaseLockAsync(MakeKey(key), token, cancellationToken);
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

        private static string MakeKey(string key) => $"LOCK_{key}";
    }
}

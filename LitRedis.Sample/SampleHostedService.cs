using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LitRedis.Sample
{
    public class SampleCacheObject
    {
        public string Phrase { get; set; } = $"Cache me if you can! {DateTime.Now}";
    }
    public class SampleHostedService(
        ILogger<SampleHostedService> logger,
        ILitRedisCacheStore redisCacheStore,
        IDistributedCache distributedCache,
        ILitRedisDistributedLockService redisDistributedLockService,
        ILitRedisJsonSerializer serializer)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DoCacheSample(stoppingToken);

            await DoDistributedCacheSample(stoppingToken);

            await DoLockNoWaitSample(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var tasks = new[]
                {
                    DoLockSample(stoppingToken),
                    DoLockSample(stoppingToken),
                    DoLockSample(stoppingToken)
                };
                await Task.WhenAll(tasks);
            }
        }

        private async Task DoLockNoWaitSample(CancellationToken stoppingToken)
        {
            var getIt = Task.Run(async () =>
            {
                var myId = Guid.NewGuid();

                var waitModel = RequestLockModel
                    .WithKey("lit-sample-no-wait")
                    .WaitForever();

                await using var locker = await redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

                if (locker.Succeeded)
                {
                    logger.LogInformation("I'm the lock master back off {@Id}", myId);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }, stoppingToken);


            var dontGetIt = Task.Run(async () =>
            {
                var myId = Guid.NewGuid();

                var waitModel = RequestLockModel
                    .WithKey("lit-sample-no-wait")
                    .NoWait();

                await using var locker = await redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

                if (locker.Succeeded)
                {
                    logger.LogInformation("I wasn't supposed to get the lock but i did :shrug: {@Id}", myId);
                }
                else
                {
                    logger.LogInformation("I didn't get the lock and that's ok {@Id}", myId);
                }
            }, stoppingToken);

            await Task.WhenAll(getIt, dontGetIt);
        }

        private async Task DoLockSample(CancellationToken stoppingToken)
        {
            try
            {
                var start = DateTime.Now;
                var myId = Guid.NewGuid();

                logger.LogInformation("I'm trying to get the lock {@Id}", myId);

                var model = RequestLockModel.WithKey("lit-sample");

                await using var locker = await redisDistributedLockService.AcquireLockAsync(model, stoppingToken);

                if (locker.Succeeded)
                {
                    logger.LogInformation("I got the lock {@Id}", myId);

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    var end = DateTime.Now;

                    logger.LogInformation("Total compute time {@Time} {@Id}", end - start, myId);
                }
                else
                {
                    logger.LogInformation("I never got the lock :( {@Id}", myId);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error running lock sample");
            }
        }

        private async Task DoCacheSample(CancellationToken stoppingToken)
        {
            try
            {
                var first = await redisCacheStore.GetAsync<SampleCacheObject>("one", stoppingToken);
                logger.LogInformation("On pulling first: {@First}", first?.Phrase);

                await redisCacheStore.PutAsync("one", new SampleCacheObject(), TimeSpan.FromMinutes(5), stoppingToken);

                logger.LogInformation("Put it");

                var second = await redisCacheStore.GetAsync<SampleCacheObject>("one", stoppingToken);
                logger.LogInformation("On pulling second: {@Second}", second?.Phrase);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error doing cache sample");
            }
        }

        private async Task DoDistributedCacheSample(CancellationToken stoppingToken)
        {
            try
            {
                var firstString = await distributedCache.GetStringAsync("one-idistributed", stoppingToken);

                if (!string.IsNullOrEmpty(firstString))
                {
                    var first = serializer.Deserialize<SampleCacheObject>(firstString);
                    logger.LogInformation("On pulling first distributed: {@First}", first?.Phrase);
                }

                await distributedCache.SetStringAsync("one-idistributed", serializer.Serialize(new SampleCacheObject()), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                }, stoppingToken);

                logger.LogInformation("Put it distributed");

                var secondString = await distributedCache.GetStringAsync("one-idistributed", stoppingToken);
                var second = serializer.Deserialize<SampleCacheObject>(secondString);
                logger.LogInformation("On pulling second distributed: {@Second}", second?.Phrase);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error doing cache sample");
            }
        }
    }
}

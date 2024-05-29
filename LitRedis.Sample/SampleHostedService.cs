using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LitRedis.Sample
{
    public class SampleCacheObject
    {
        public string Phrase { get; set; } = $"Cache me if you can! {DateTime.Now}";
    }
    public class SampleHostedService : BackgroundService
    {
        private readonly ILitRedisCacheStore _redisCacheStore;
        private readonly ILitRedisDistributedLockService _redisDistributedLockService;
        private readonly ILogger<SampleHostedService> _logger;

        public SampleHostedService(ILogger<SampleHostedService> logger,
            ILitRedisCacheStore redisCacheStore,
            ILitRedisDistributedLockService redisDistributedLockService)
        {
            _logger = logger;
            _redisCacheStore = redisCacheStore;
            _redisDistributedLockService = redisDistributedLockService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DoCacheSample(stoppingToken);

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

                await _redisCacheStore.PutAsync("gone", new SampleCacheObject(), TimeSpan.FromMinutes(5), stoppingToken);

                var shouldNotBeGone = await _redisCacheStore.GetAsync<SampleCacheObject>("gone", stoppingToken);
                _logger.LogInformation("Should not be gone: {@NotGone}", shouldNotBeGone);

                await _redisCacheStore.ClearAllAsync(stoppingToken);

                _logger.LogInformation("Cleared all cache");

                var shouldBeGone = await _redisCacheStore.GetAsync<SampleCacheObject>("gone", stoppingToken);
                _logger.LogInformation("Should be gone: {@Gone}", shouldBeGone);

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

                var locker = await _redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

                if (locker.Succeeded)
                {
                    _logger.LogInformation("I'm the lock master back off {@Id}", myId);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    await locker.DisposeAsync();
                }
            }, stoppingToken);


            var dontGetIt = Task.Run(async () =>
            {
                await Task.Delay(500, stoppingToken);

                var myId = Guid.NewGuid();

                var waitModel = RequestLockModel
                    .WithKey("lit-sample-no-wait")
                    .NoWait();

                await using var locker = await _redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

                if (locker.Succeeded)
                {
                    _logger.LogInformation("I wasn't supposed to get the lock but i did :shrug: {@Id}", myId);
                }
                else
                {
                    _logger.LogInformation("I didn't get the lock and that's ok {@Id}", myId);
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

                _logger.LogInformation("I'm trying to get the lock {@Id}", myId);

                var model = RequestLockModel.WithKey("lit-sample");

                await using var locker = await _redisDistributedLockService.AcquireLockAsync(model, stoppingToken);

                if (locker.Succeeded)
                {
                    _logger.LogInformation("I got the lock {@Id}", myId);

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    var end = DateTime.Now;

                    _logger.LogInformation("Total compute time {@Time} {@Id}", end - start, myId);
                }
                else
                {
                    _logger.LogInformation("I never got the lock :( {@Id}", myId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error running lock sample");
            }
        }

        private async Task DoCacheSample(CancellationToken stoppingToken)
        {
            try
            {
                const string key = "one";

                var first = await _redisCacheStore.GetAsync<SampleCacheObject>(key, stoppingToken);
                _logger.LogInformation("On pulling first: {@First}", first?.Phrase);

                await _redisCacheStore.PutAsync(key, new SampleCacheObject(), TimeSpan.FromMinutes(5), stoppingToken);

                _logger.LogInformation("Put it");

                var second = await _redisCacheStore.GetAsync<SampleCacheObject>(key, stoppingToken);
                _logger.LogInformation("On pulling second: {@Second}", second?.Phrase);

                await _redisCacheStore.SetExpiryAsync<SampleCacheObject>(key, TimeSpan.FromSeconds(5), stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                var expired = await _redisCacheStore.GetAsync<SampleCacheObject>(key, stoppingToken);
                _logger.LogInformation("On pulling expired: {@Expired}", expired?.Phrase);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error doing cache sample");
            }
        }
    }
}

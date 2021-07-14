- [lit-redis](#lit-redis)
- [Setup](#setup)
- [Usage](#usage)
  - [Caching](#caching)
    - [PutAsync(string key, SampleCacheObject model, TimeSpan? expiry, CancellationToken cancellationToken)](#putasyncstring-key-samplecacheobject-model-timespan-expiry-cancellationtoken-cancellationtoken)
    - [GetAsync(string key, CancellationToken cancellationToken)](#getasyncstring-key-cancellationtoken-cancellationtoken)
  - [Distributed locking](#distributed-locking)
    - [With wait](#with-wait)
      - [Default](#default)
      - [Wait forever](#wait-forever)
      - [Set the wait increase, timeout, and interval](#set-the-wait-increase-timeout-and-interval)
    - [No wait](#no-wait)

# lit-redis
A C# managed Redis Library for doing caching, locking, and concurrency

# Setup

1. Install the library
```xml
<ItemGroup>
   <PackageReference Include="Firebend.LitRedis.Core" />
</ItemGroup>
```
or 
```bash
dotnet add package Firebend.LitRedis.Core
```

2. In `Program.cs`, add the Lit Redis configuration to the `ConfigureServices` callback in `CreateHostBuilder`
```csharp
   services
     .AddLitRedis(redis => redis.WithCaching().WithLocking().WithConnectionString("localhost:6379,defaultDatabase=0"))
     .AddHostedService<SampleHostedService>()
     .AddLogging(o => o.AddSimpleConsole(c => c.TimestampFormat = "[yyy-MM-dd HH:mm:ss] "));
```

# Usage

Using the following `SampleHostedService` extending `BackgroundService`

```csharp
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

   protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
   // execute
   }
}
```

## Caching

Create a `SampleCacheObject` class defining the data structure to write to the cache

```csharp
public class SampleCacheObject
{
   public string Phrase { get; set; } = $"Cache me if you can! {DateTime.Now}";
}
```

### PutAsync(string key, SampleCacheObject model, TimeSpan? expiry, CancellationToken cancellationToken)

Write an object to the cache, providing the key to store data under as the first argument

```csharp
try {
  await _redisCacheStore.PutAsync("one", new SampleCacheObject(), TimeSpan.FromMinutes(5), stoppingToken);
}
catch (Exception ex) {
  _logger.LogCritical(ex, "Error");
}
```

### GetAsync(string key, CancellationToken cancellationToken)

Read a cached object from the store, providing the key data is stored under as the first argument

```csharp
try {
  var data = await _redisCacheStore.GetAsync<SampleCacheObject>("one", stoppingToken);
  _logger.LogInformation($"Phrase: {data?.Phrase}");
}
catch (Exception ex) {
  _logger.LogCritical(ex, "Error");
}
```

## Distributed locking

### With wait

Attempt to acquire a lock on a particular key, waiting until the lock is able to be acquired.

#### Default

By default, `LockIncrease` and `RenewLockInterval` are both set to 10 seconds.

```csharp
try {
   var waitModel = RequestLockModel
      .WithKey("lit-sample")

   await using var locker = await _redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

   if (locker.Succeeded)
   {
      _logger.LogInformation("Lock acquired");
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
   }
}
catch (Exception ex) {
   _logger.LogCritical(ex, "Error");
}
```

#### Wait forever

Use `WaitForever` or set the lock model class's `WaitTimeout` to `null`

```csharp
try {
   var waitModel = RequestLockModel
      .WithKey("lit-sample")
      .WaitForever();

   await using var locker = await _redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

   if (locker.Succeeded)
   {
      _logger.LogInformation("Lock acquired");
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
   }
}
catch (Exception ex) {
   _logger.LogCritical(ex, "Error");
}
```

#### Set the wait increase, timeout, and interval

Use `WithLockIncrease`, `WithRenewLockInterval`, and `WithLockWaitTimeout` or set the lock model class's `LockIncrease`, `RenewLockInterval`, and `WaitTimeout` to `TimeSpan`s

```csharp
try {
   var waitModel = RequestLockModel
      .WithKey("lit-sample")
      .WithLockIncrease(TimeSpan.FromSeconds(3))
      .WithRenewLockInterval(TimeSpan.FromSeconds(3))
      .WithLockWaitTimeout(TimeSpan.FromSeconds(20));

   await using var locker = await _redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

   if (locker.Succeeded)
   {
      _logger.LogInformation("Lock acquired");
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
   }
}
catch (Exception ex) {
   _logger.LogCritical(ex, "Error");
}
```

### No wait

Use `NoWait` or set the lock model class's `WaitTimeout` to `0`. If the lock fails to be acquired, it will simply exit

```csharp
try {
   var waitModel = RequestLockModel
      .WithKey("lit-sample")
      .NoWait();

   await using var locker = await _redisDistributedLockService.AcquireLockAsync(waitModel, stoppingToken);

   if (locker.Succeeded)
   {
      _logger.LogInformation("Lock acquired");
   } 
   else {
      _logger.LogInformation("No lock acquired");
   }
}
catch (Exception ex) {
   _logger.LogCritical(ex, "Error");
}
```
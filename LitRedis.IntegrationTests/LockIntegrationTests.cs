using FluentAssertions;
using LitRedis.Core;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LitRedis.IntegrationTests;

[TestClass]
public class LockIntegrationTests
{
    private readonly ILitRedisDistributedLockService _lock;
    private readonly string _key;
    public LockIntegrationTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(_ => { });
        serviceCollection.AddLitRedis("localhost:6379,defaultDatabase=0", redis => redis.WithCaching().WithLocking());

        var serviceProvider = serviceCollection.BuildServiceProvider();

        _lock = serviceProvider.GetRequiredService<ILitRedisDistributedLockService>();

        _key = Guid.NewGuid().ToString();
    }

    [TestMethod]
    public async Task Lock_Should_Get()
    {
        var result = await _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).NoWait(), default);
        result.Succeeded.Should().BeTrue();
        await result.DisposeAsync();
    }

    [TestMethod]
    public async Task Lock_Should_Not_Get()
    {
        var result = await _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).NoWait(), default);
        result.Succeeded.Should().BeTrue();

        var badResult = await _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).NoWait(), default);
        badResult.Succeeded.Should().BeFalse();

        await result.DisposeAsync();
        await badResult.DisposeAsync();
    }

    [TestMethod]
    public async Task Lock_Should_Not_Get_Timeout()
    {
        var result = await _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).NoWait(), default);
        result.Succeeded.Should().BeTrue();

        var task = _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).WithLockWaitTimeout(TimeSpan.FromSeconds(5)), default);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var badResult = await task;
        badResult.Succeeded.Should().BeFalse();

        await result.DisposeAsync();
        await badResult.DisposeAsync();
    }

    [TestMethod]
    public async Task Lock_Should_Get_Timeout()
    {
        var result = await _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).NoWait(), default);
        result.Succeeded.Should().BeTrue();

        var task = _lock.AcquireLockAsync(RequestLockModel.WithKey(_key).WithLockWaitTimeout(TimeSpan.FromSeconds(5)), default);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await result.DisposeAsync();

        var goodResult = await task;
        goodResult.Succeeded.Should().BeTrue();
    }
}

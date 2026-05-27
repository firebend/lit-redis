using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using LitRedis.Core.Exceptions;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LitRedis.Tests.Core.Implementations;

[TestClass]
public class LitRedisDistributedLockServiceTests
{
    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Service_Should_Acquire_Lock()
    {
        //arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var wrapper = fixture.Freeze<Mock<ILitRedisDistributedLock>>();
        wrapper.Setup(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var redisDistributedLock = fixture.Create<LitRedisDistributedLockService>();

        var model = RequestLockModel
            .WithKey("fake")
            .WithLockWaitTimeout(TimeSpan.FromSeconds(1));

        //act
        var result = await redisDistributedLock.AcquireLockAsync(model, default);

        //assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Service_Should_Not_Acquire_Lock()
    {
        //arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var wrapper = fixture.Freeze<Mock<ILitRedisDistributedLock>>();
        wrapper.Setup(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var redisDistributedLock = fixture.Create<LitRedisDistributedLockService>();

        var model = RequestLockModel
            .WithKey("fake")
            .WithLockWaitTimeout(TimeSpan.FromSeconds(1));

        //act
        var result = await redisDistributedLock.AcquireLockAsync(model, default);

        //assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        wrapper.Verify(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Service_Should_Mark_Lock_Lost_When_Lock_Fails_To_Extend()
    {
        //arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var wrapper = fixture.Freeze<Mock<ILitRedisDistributedLock>>();
        wrapper.Setup(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        wrapper.Setup(x => x.ExtendLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var redisDistributedLock = fixture.Create<LitRedisDistributedLockService>();

        var model = RequestLockModel
            .WithKey("fake")
            .WithLockWaitTimeout(TimeSpan.FromSeconds(1))
            .WithRenewLockInterval(TimeSpan.FromMilliseconds(100));

        //act
        await using var locker = await redisDistributedLock.AcquireLockAsync(model, default);

        locker.Succeeded.Should().BeTrue();

        // Wait for the renewal loop to run: delay (100ms) + extend call + mark lost
        await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.CancellationTokenSource.Token);

        //assert
        locker.Status.Should().Be(LitRedisLockStatus.Lost);
        locker.LockLostToken.IsCancellationRequested.Should().BeTrue();

        var throwOnLost = locker.ThrowOnLockLost;
        throwOnLost.Should().Throw<LockLostException>();

        // Verify ExtendLockAsync was called (diagnostic)
        wrapper.Verify(x => x.ExtendLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // Disposing a lost lock must not attempt to release the key on the server.
        await locker.DisposeAsync();
        wrapper.Verify(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public TestContext TestContext { get; set; }
}

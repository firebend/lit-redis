using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;

namespace LitRedis.Tests.Core.Implementations
{
    [TestClass]
    public class LitRedisDistributedLockTests
    {
        [TestMethod]
        public async Task Lit_Redis_Distributed_Lock_Should_Take_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisConnectionService>>();
            wrapper.Setup(x => x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLock>();

            //act
            var result = await redisDistributedLock.TakeLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

            //assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task Lit_Redis_Distributed_Lock_Should_Not_Take_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisConnectionService>>();
            wrapper.Setup(x => x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLock>();

            //act
            var result = await redisDistributedLock.TakeLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

            //assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task Lit_Redis_Distributed_Lock_Should_Release_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisConnectionService>>();
            wrapper.Setup(x => x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLock>();

            //act
            var result = await redisDistributedLock.ReleaseLockAsync("fake", "fake token", default);

            //assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task Lit_Redis_Distributed_Lock_Should_Not_Release_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisConnectionService>>();
            wrapper.Setup(x => x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLock>();

            //act
            var result = await redisDistributedLock.ReleaseLockAsync("fake", "fake token", default);

            //assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task Lit_Redis_Distributed_Lock_Should_Extend_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisConnectionService>>();
            wrapper.Setup(x => x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLock>();

            //act
            var result = await redisDistributedLock.ExtendLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

            //assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task Lit_Redis_Distributed_Lock_Should_Not_Extend_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisConnectionService>>();
            wrapper.Setup(x => x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLock>();

            //act
            var result = await redisDistributedLock.ExtendLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

            //assert
            result.Should().BeFalse();
        }
    }
}

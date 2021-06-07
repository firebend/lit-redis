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

namespace LitRedis.Tests.Core.Implementations
{
    [TestClass]
    public class DistributedLockServiceTests
    {
        [TestMethod]
        public async Task Distributed_Lock_Service_Should_Acquire_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisDistributedLock>>();
            wrapper.Setup(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLockService>();

            //act
            var result = await redisDistributedLock.AcquireLockAsync("fake", TimeSpan.FromSeconds(1));

            //assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
        }

        [TestMethod]
        public async Task Distributed_Lock_Service_Should_Not_Acquire_Lock()
        {
            //arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var wrapper = fixture.Freeze<Mock<ILitRedisDistributedLock>>();
            wrapper.Setup(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var redisDistributedLock = fixture.Create<LitRedisDistributedLockService>();

            //act
            var result = await redisDistributedLock.AcquireLockAsync("fake", TimeSpan.FromSeconds(5));

            //assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            wrapper.Verify(x => x.TakeLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }
    }
}

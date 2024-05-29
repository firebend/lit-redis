using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using LitRedis.Core.Builders;
using LitRedis.Core.Implementations;
using LitRedis.Core.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;

namespace LitRedis.Tests.Core.Implementations;

[TestClass]
public class LitRedisDistributedLockTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IDatabase> _database;
    private readonly Mock<IConnectionMultiplexer> _connection;
    private readonly Mock<ILitRedisConnectionMultiplexerProvider> _providerMock;

    public LitRedisDistributedLockTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _database = _fixture.Freeze<Mock<IDatabase>>();

        _connection = _fixture.Freeze<Mock<IConnectionMultiplexer>>();
        _connection.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_database.Object);

        _providerMock = _fixture.Freeze<Mock<ILitRedisConnectionMultiplexerProvider>>();
        _providerMock.Setup(x => x.GetConnectionMultiplexerAsync())
            .ReturnsAsync(_connection.Object);
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Should_Take_Lock()
    {
        //arrange
        _database.Setup(x =>x.LockTakeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var redisDistributedLock = _fixture.Create<LitRedisDistributedLock>();

        //act
        var result = await redisDistributedLock.TakeLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

        //assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Should_Not_Take_Lock()
    {
        //arrange
        _database.Setup(x =>x.LockTakeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var redisDistributedLock = _fixture.Create<LitRedisDistributedLock>();

        //act
        var result = await redisDistributedLock.TakeLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

        //assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Should_Release_Lock()
    {
        //arrange
        _database.Setup(x =>x.LockReleaseAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var redisDistributedLock = _fixture.Create<LitRedisDistributedLock>();

        //act
        var result = await redisDistributedLock.ReleaseLockAsync("fake", "fake token", default);

        //assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Should_Not_Release_Lock()
    {
        //arrange
        _database.Setup(x =>x.LockReleaseAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var redisDistributedLock = _fixture.Create<LitRedisDistributedLock>();

        //act
        var result = await redisDistributedLock.ReleaseLockAsync("fake", "fake token", default);

        //assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Should_Extend_Lock()
    {
        //arrange
        _database.Setup(x =>x.LockExtendAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var redisDistributedLock = _fixture.Create<LitRedisDistributedLock>();

        //act
        var result = await redisDistributedLock.ExtendLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

        //assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Lit_Redis_Distributed_Lock_Should_Not_Extend_Lock()
    {
        //arrange
        _database.Setup(x =>x.LockExtendAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var redisDistributedLock = _fixture.Create<LitRedisDistributedLock>();

        //act
        var result = await redisDistributedLock.ExtendLockAsync("fake", "fake token", TimeSpan.FromSeconds(1), default);

        //assert
        result.Should().BeFalse();
    }
}

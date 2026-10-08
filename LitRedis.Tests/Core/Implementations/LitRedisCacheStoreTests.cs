using System;
using System.Text.Json;
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

namespace LitRedis.Tests.Core.Implementations;

[TestClass]
public class LitRedisCacheStoreTests
{
    private class FakeCacheObject
    {
        public string Value { get; set; }
    }

    [TestMethod]
    public async Task Lit_Redis_Cache_Store_Should_Get_Value()
    {
        //arrange
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var mockRedisConnectionService = fixture.Freeze<Mock<ILitRedisConnectionService>>();

        mockRedisConnectionService.Setup(x =>
                x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<RedisValue>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedisValue("fake"));

        var cacheStore = fixture.Create<LitRedisCacheStore>();

        //act
        var value = await cacheStore.GetAsync("fake-key", default);

        //assert
        value.Should().Be("fake");

        mockRedisConnectionService.Verify(x =>
            x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<RedisValue>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Lit_Redis_Cache_Store_Should_Get_Value_Object()
    {
        //arrange
        var cacheValue = new FakeCacheObject { Value = "Fakey Fake" };

        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var mockRedisConnectionService = fixture.Freeze<Mock<ILitRedisConnectionService>>();

        fixture.Inject<ILitRedisJsonSerializer>(new LitRedisSystemTextJsonSerializer(new DefaultLitRedisSystemTextJsonOptionsProvider(new JsonSerializerOptions(JsonSerializerDefaults.Web))));

        mockRedisConnectionService.Setup(x =>
                x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<RedisValue>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedisValue(JsonSerializer.Serialize(cacheValue)));

        var cacheStore = fixture.Create<LitRedisCacheStore>();

        //act
        var value = await cacheStore.GetAsync<FakeCacheObject>("fake-key", default);

        //assert
        value.Should().NotBeNull();
        value.Value.Should().Be("Fakey Fake");

        mockRedisConnectionService.Verify(x =>
            x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<RedisValue>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Lit_Redis_Cache_Store_Should_Put_Value_Object()
    {
        //arrange
        var cacheValue = new FakeCacheObject { Value = "Fakey Fake" };

        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var mockRedisConnectionService = fixture.Freeze<Mock<ILitRedisConnectionService>>();

        mockRedisConnectionService.Setup(x =>
                x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cacheStore = fixture.Create<LitRedisCacheStore>();

        //act
        await cacheStore.PutAsync("fake-key", cacheValue, null, default);

        //assert
        mockRedisConnectionService.Verify(x =>
            x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Lit_Redis_Cache_Store_Should_Put_Without_Expiry_When_Expiry_Is_Null()
    {
        //act
        var expiration = await PutAndCaptureExpirationAsync(null);

        //assert
        // Expiration.Default sends a plain SET, which clears any existing TTL (it is not KEEPTTL).
        expiration.Should().Be(Expiration.Default);
        expiration.Should().NotBe(Expiration.KeepTtl);
    }

    [TestMethod]
    public async Task Lit_Redis_Cache_Store_Should_Put_With_Relative_Expiry_When_Expiry_Is_Set()
    {
        //arrange
        var expiry = TimeSpan.FromMinutes(5);

        //act
        var expiration = await PutAndCaptureExpirationAsync(expiry);

        //assert
        expiration.Should().Be(new Expiration(expiry));
    }

    private static async Task<Expiration> PutAndCaptureExpirationAsync(TimeSpan? expiry)
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var mockRedisConnectionService = fixture.Freeze<Mock<ILitRedisConnectionService>>();
        var mockDatabase = new Mock<IDatabase>();
        Expiration? captured = null;

        mockDatabase.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(), It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, _, expiration, _, _) => captured = expiration)
            .ReturnsAsync(true);

        mockRedisConnectionService.Setup(x =>
                x.UseDbAsync(It.IsAny<Func<IDatabase, CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<IDatabase, CancellationToken, Task<bool>>, CancellationToken>((func, ct) => func(mockDatabase.Object, ct));

        var cacheStore = fixture.Create<LitRedisCacheStore>();

        await cacheStore.PutAsync("fake-key", new FakeCacheObject { Value = "Fakey Fake" }, expiry, default);

        mockDatabase.Verify(x => x.StringSetAsync(
            "fake-key", It.IsAny<RedisValue>(), It.IsAny<Expiration>(), It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Once);

        captured.Should().NotBeNull();

        return captured.Value;
    }
}

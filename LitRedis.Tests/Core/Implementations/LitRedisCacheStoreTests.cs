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

namespace LitRedis.Tests.Core.Implementations
{
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
            var cacheValue = new FakeCacheObject {Value = "Fakey Fake"};

            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var mockRedisConnectionService = fixture.Freeze<Mock<ILitRedisConnectionService>>();

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
            var cacheValue = new FakeCacheObject {Value = "Fakey Fake"};

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
    }
}

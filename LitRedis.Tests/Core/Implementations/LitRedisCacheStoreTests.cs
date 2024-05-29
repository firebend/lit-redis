using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using LitRedis.Core.Implementations;
using LitRedis.Tests.Mocks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        var cacheValue = new FakeCacheObject { Value = "Fakey Fake" };
        var cache = new HybridCacheMock();
        await cache.SetAsync("fake-key", cacheValue);

        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());
        fixture.Inject<HybridCache>(cache);

        var cacheStore = fixture.Create<LitRedisCacheStore>();

        //act
        var value = await cacheStore.GetAsync<FakeCacheObject>("fake-key", default);

        //assert
        value.Should().NotBeNull();
        value.Value.Should().Be("Fakey Fake");
    }

    [TestMethod]
    public async Task Lit_Redis_Cache_Store_Should_Put_Value_Object()
    {
        //arrange
        var cacheValue = new FakeCacheObject { Value = "Fakey Fake" };
        var cacheMock = new HybridCacheMock();

        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());
        fixture.Inject<HybridCache>(cacheMock);

        var cacheStore = fixture.Create<LitRedisCacheStore>();

        //act
        await cacheStore.PutAsync("fake-key", cacheValue, null, default);

        //assert
        cacheMock.Dictionary["fake-key"].Should().NotBeNull();
    }
}

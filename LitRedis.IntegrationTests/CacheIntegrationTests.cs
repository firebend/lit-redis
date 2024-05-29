using FluentAssertions;
using LitRedis.Core;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LitRedis.IntegrationTests;

[TestClass]
public class CacheIntegrationTests
{
    private readonly ILitRedisCacheStore _cacheStore;
    private readonly string _key;
    private readonly TimeSpan _expiry = TimeSpan.FromSeconds(30);

    public class TestModel(string str)
    {
        public string Str { get; set; } = str;
    }

    public CacheIntegrationTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLitRedis("localhost:6379,defaultDatabase=0", redis => redis.WithCaching().WithLocking());

        var serviceProvider = serviceCollection.BuildServiceProvider();

        _cacheStore = serviceProvider.GetRequiredService<ILitRedisCacheStore>();

        _key = Guid.NewGuid().ToString();
    }

    [TestMethod]
    public async Task Cache_Should_Put()
    {
        await _cacheStore.PutAsync(_key, new TestModel("hello"), _expiry, default);
        var result = await _cacheStore.GetAsync<TestModel>(_key, default);
        result.Should().NotBeNull();
        result.Str.Should().Be("hello");
    }

    [TestMethod]
    public async Task Cache_Should_Clear()
    {
        await _cacheStore.PutAsync(_key, new TestModel("hello"), _expiry, default);
        await _cacheStore.ClearAsync(_key, default);
        var result = await _cacheStore.GetAsync<TestModel>(_key, default);
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Cache_Should_Clear_All()
    {
        await _cacheStore.PutAsync(_key, new TestModel("hello"), null, default);
        await _cacheStore.ClearAllAsync(default);
        var result = await _cacheStore.GetAsync<TestModel>(_key, default);
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Cache_Should_Get_All_Keys()
    {
        string[] keys = ["1", "2", "3", "4", "5"];

        foreach (var key in keys)
        {
            await _cacheStore.PutAsync(key, new TestModel(key), _expiry, default);
        }

        var result = await _cacheStore.GetAllKeys(default);

        result.Should().Contain(keys);
    }

    [TestMethod]
    public async Task Cache_Should_Expire()
    {
        await _cacheStore.PutAsync(_key, new TestModel("hello"), TimeSpan.FromSeconds(5), default);
        var fetched = await _cacheStore.GetAsync<TestModel>(_key, default);
        fetched.Should().NotBeNull();
        await Task.Delay(TimeSpan.FromSeconds(10));
        fetched = await _cacheStore.GetAsync<TestModel>(_key, default);
        fetched.Should().BeNull();
    }

    [TestMethod]
    public async Task Cache_Should_Change_Expiry()
    {
        var str = Guid.NewGuid().ToString();
        await _cacheStore.PutAsync(_key, new TestModel(str), null, default);

        var fetched = await _cacheStore.GetAsync<TestModel>(_key, default);
        fetched.Should().NotBeNull();

        await _cacheStore.SetExpiryAsync<TestModel>(_key, TimeSpan.FromSeconds(5), default);

        fetched = await _cacheStore.GetAsync<TestModel>(_key, default);
        fetched.Should().NotBeNull();
        fetched.Str.Should().Be(str);

        await Task.Delay(TimeSpan.FromSeconds(10));

        fetched = await _cacheStore.GetAsync<TestModel>(_key, default);
        fetched.Should().BeNull();
    }
}

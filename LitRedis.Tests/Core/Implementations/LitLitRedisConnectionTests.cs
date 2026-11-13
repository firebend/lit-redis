using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LitRedis.Core.Implementations;
using LitRedis.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace LitRedis.Tests.Core.Implementations;

[TestClass]
public class LitLitRedisConnectionTests
{
    // abortConnect=false makes ConnectAsync return a multiplexer without a reachable server,
    // so these tests need no Redis: only the factory decides whether a connect attempt fails.
    private const string UnreachableConnectionString = "127.0.0.1:1,abortConnect=false";

    private static LitRedisOptions CreateOptions(Func<ConfigurationOptions, Task<ConfigurationOptions>> factory) => new()
    {
        ConnectionString = UnreachableConnectionString,
        RedisConnectTimeout = 200,
        ConfigurationOptionsFactory = factory
    };

    [TestMethod]
    public async Task GetConnectionMultiplexer_Should_Retry_After_The_First_Connect_Fails()
    {
        var attempts = 0;
        var connection = new LitLitRedisConnection(CreateOptions(options =>
            Interlocked.Increment(ref attempts) == 1
                ? throw new InvalidOperationException("token fetch failed")
                : Task.FromResult(options)));

        var first = () => connection.GetConnectionMultiplexer();
        await first.Should().ThrowAsync<InvalidOperationException>();

        using var multiplexer = await connection.GetConnectionMultiplexer();

        multiplexer.Should().NotBeNull();
        attempts.Should().Be(2);
    }

    [TestMethod]
    public async Task GetConnectionMultiplexer_Should_Reuse_A_Successful_Connection()
    {
        var attempts = 0;
        var connection = new LitLitRedisConnection(CreateOptions(options =>
        {
            Interlocked.Increment(ref attempts);
            return Task.FromResult(options);
        }));

        using var first = await connection.GetConnectionMultiplexer();
        var second = await connection.GetConnectionMultiplexer();

        second.Should().BeSameAs(first);
        attempts.Should().Be(1);
    }

    [TestMethod]
    public async Task GetConnectionMultiplexer_Should_Share_One_Retry_Between_Concurrent_Callers()
    {
        var attempts = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new LitLitRedisConnection(CreateOptions(async options =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw new InvalidOperationException("token fetch failed");
            }

            await release.Task;
            return options;
        }));

        var first = () => connection.GetConnectionMultiplexer();
        await first.Should().ThrowAsync<InvalidOperationException>();

        var callers = Enumerable.Range(0, 10).Select(_ => connection.GetConnectionMultiplexer()).ToArray();
        release.SetResult();
        var multiplexers = await Task.WhenAll(callers);

        multiplexers.Distinct().Should().ContainSingle();
        attempts.Should().Be(2);
        multiplexers[0].Dispose();
    }
}

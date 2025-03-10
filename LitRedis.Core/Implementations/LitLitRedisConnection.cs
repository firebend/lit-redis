using System;
using System.Threading;
using System.Threading.Tasks;
using LitRedis.Core.Interfaces;
using LitRedis.Core.Models;
using StackExchange.Redis;

namespace LitRedis.Core.Implementations;

/// <summary>
/// This class should be registered as a singleton in your IoC container.
/// It handles connection and reconnecting to Redis.
/// </summary>
public class LitLitRedisConnection : ILitRedisConnection
{
    private long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
    private DateTimeOffset _firstError = DateTimeOffset.MinValue;
    private DateTimeOffset _previousError = DateTimeOffset.MinValue;
    private Lazy<Task<ConnectionMultiplexer>> _multiplexer;

    private readonly object _reconnectLock = new();
    private readonly LitRedisOptions _litRedisOptions;

    public LitLitRedisConnection(LitRedisOptions litRedisOptions)
    {
        _litRedisOptions = litRedisOptions;
        _multiplexer = CreateMultiplexer();
    }

    public Task<ConnectionMultiplexer> GetConnectionMultiplexer() => _multiplexer.Value;

    ///<summary>
    /// Force a new ConnectionMultiplexer to be created.
    /// NOTES:
    ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnect()
    ///     2. Don't call ForceReconnect for Timeouts, just for RedisConnectionExceptions or SocketExceptions
    ///     3. Call this method every time you see a connection exception, the code will wait to reconnect:
    ///         a. for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
    ///         b. not reconnect more frequently than configured in "ReconnectMinFrequency"
    ///</summary>
    public void ForceReconnect()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var previousTicks = Interlocked.Read(ref _lastReconnectTicks);
        var previousReconnect = new DateTimeOffset(previousTicks, TimeSpan.Zero);
        var elapsedSinceLastReconnect = utcNow - previousReconnect;

        // If multiple threads call ForceReconnect at the same time, we only want to honor one of them.
        if (elapsedSinceLastReconnect <= _litRedisOptions.ReconnectMinFrequency)
        {
            return;
        }

        lock (_reconnectLock)
        {
            utcNow = DateTimeOffset.UtcNow;
            elapsedSinceLastReconnect = utcNow - previousReconnect;

            if (_firstError == DateTimeOffset.MinValue)
            {
                // We haven't seen an error since last reconnect, so set initial values.
                _firstError = utcNow;
                _previousError = utcNow;
                return;
            }

            if (elapsedSinceLastReconnect < _litRedisOptions.ReconnectMinFrequency)
            {
                return; // Some other thread made it through the check and the lock, so nothing to do.
            }

            var elapsedSinceFirstError = utcNow - _firstError;
            var elapsedSinceMostRecentError = utcNow - _previousError;

            var shouldReconnect =
                elapsedSinceFirstError >= _litRedisOptions.ReconnectErrorThreshold // make sure we gave the multiplexer enough time to reconnect on its own if it can
                && elapsedSinceMostRecentError <=
                _litRedisOptions.ReconnectErrorThreshold; //make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

            // Update the previousError timestamp to be now (e.g. this request to reconnect)
            _previousError = utcNow;

            if (!shouldReconnect)
            {
                return;
            }

            _firstError = DateTimeOffset.MinValue;
            _previousError = DateTimeOffset.MinValue;

            var oldMultiplexer = _multiplexer;
            _multiplexer = CreateMultiplexer();
            CloseMultiplexer(oldMultiplexer).ConfigureAwait(false).GetAwaiter().GetResult();
            Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
        }
    }

    private Lazy<Task<ConnectionMultiplexer>> CreateMultiplexer()
    {
        return new Lazy<Task<ConnectionMultiplexer>>(async () =>
        {
            var options = ConfigurationOptions.Parse(_litRedisOptions.ConnectionString);
            options.AsyncTimeout = _litRedisOptions.RedisAsyncTimeout;
            options.ConnectTimeout = _litRedisOptions.RedisConnectTimeout;
            options.SyncTimeout = _litRedisOptions.RedisSyncTimeout;
            options.ReconnectRetryPolicy = new ExponentialRetry(_litRedisOptions.RedisDeltaBackOffMilliseconds);
            if (_litRedisOptions.ConfigurationOptionsFactory != null)
            {
                options = await _litRedisOptions.ConfigurationOptionsFactory.Invoke(options);
            }

            return await ConnectionMultiplexer.ConnectAsync(options);
        });
    }

    private static async Task CloseMultiplexer(Lazy<Task<ConnectionMultiplexer>> oldMultiplexer)
    {
        if (oldMultiplexer == null)
        {
            return;
        }

        try
        {
            await (await oldMultiplexer.Value).CloseAsync();
        }
        catch (Exception)
        {
            // Example error condition: if accessing old.Value causes a connection attempt and that fails.
        }
    }
}

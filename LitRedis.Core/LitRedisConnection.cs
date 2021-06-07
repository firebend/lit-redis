using System;
using System.Threading;
using StackExchange.Redis;

namespace LitRedis.Core
{
    public class LitRedisConnection
    {
        private static long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
        private static DateTimeOffset _firstError = DateTimeOffset.MinValue;
        private static DateTimeOffset _previousError = DateTimeOffset.MinValue;

        private static readonly object ReconnectLock = new();

        // In general, let StackExchange.Redis handle most reconnects,
        // so limit the frequency of how often this will actually reconnect.
        public static readonly TimeSpan ReconnectMinFrequency = TimeSpan.FromSeconds(60);

        // if errors continue for longer than the below threshold, then the
        // multiplexer seems to not be reconnecting, so re-create the multiplexer
        public static readonly TimeSpan ReconnectErrorThreshold = TimeSpan.FromSeconds(30);

        private static string _connectionString = "TODO: CALL InitializeConnectionString() method with connection string";
        private static Lazy<ConnectionMultiplexer> _multiplexer = CreateMultiplexer();

        public static ConnectionMultiplexer Connection => _multiplexer.Value;

        public static void InitializeConnectionString(string cnxString)
        {
            if (string.IsNullOrWhiteSpace(cnxString))
            {
                throw new ArgumentNullException(nameof(cnxString));
            }

            _connectionString = cnxString;
        }

        ///<summary>
        /// Force a new ConnectionMultiplexer to be created.
        /// NOTES:
        ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnect()
        ///     2. Don't call ForceReconnect for Timeouts, just for RedisConnectionExceptions or SocketExceptions
        ///     3. Call this method every time you see a connection exception, the code will wait to reconnect:
        ///         a. for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
        ///         b. not reconnect more frequently than configured in "ReconnectMinFrequency"
        ///</summary>
        public static void ForceReconnect()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var previousTicks = Interlocked.Read(ref _lastReconnectTicks);
            var previousReconnect = new DateTimeOffset(previousTicks, TimeSpan.Zero);
            var elapsedSinceLastReconnect = utcNow - previousReconnect;

            // If multiple threads call ForceReconnect at the same time, we only want to honor one of them.
            if (elapsedSinceLastReconnect <= ReconnectMinFrequency)
            {
                return;
            }

            lock (ReconnectLock)
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

                if (elapsedSinceLastReconnect < ReconnectMinFrequency)
                {
                    return; // Some other thread made it through the check and the lock, so nothing to do.
                }

                var elapsedSinceFirstError = utcNow - _firstError;
                var elapsedSinceMostRecentError = utcNow - _previousError;

                var shouldReconnect =
                    elapsedSinceFirstError >= ReconnectErrorThreshold // make sure we gave the multiplexer enough time to reconnect on its own if it can
                    && elapsedSinceMostRecentError <=
                    ReconnectErrorThreshold; //make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

                // Update the previousError timestamp to be now (e.g. this reconnect request)
                _previousError = utcNow;

                if (!shouldReconnect)
                {
                    return;
                }

                _firstError = DateTimeOffset.MinValue;
                _previousError = DateTimeOffset.MinValue;

                var oldMultiplexer = _multiplexer;
                _multiplexer = CreateMultiplexer();
                CloseMultiplexer(oldMultiplexer);
                Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
            }
        }

        public static int RedisDeltaBackOffMilliseconds = 5_000;
        public static int RedisAsyncTimeout = 10_000;
        public static int RedisConnectTimeout = 10_000;
        public static int RedisSyncTimeout = 10_000;

        private static Lazy<ConnectionMultiplexer> CreateMultiplexer()
            => new(() =>
            {
                var options = ConfigurationOptions.Parse(_connectionString);
                options.AsyncTimeout = RedisAsyncTimeout;
                options.ConnectTimeout = RedisConnectTimeout;
                options.SyncTimeout = RedisSyncTimeout;
                options.ReconnectRetryPolicy = new ExponentialRetry(RedisDeltaBackOffMilliseconds);
                return ConnectionMultiplexer.Connect(options);
            });

        private static void CloseMultiplexer(Lazy<ConnectionMultiplexer> oldMultiplexer)
        {
            if (oldMultiplexer == null)
            {
                return;
            }

            try
            {
                oldMultiplexer.Value.Close();
            }
            catch (Exception)
            {
                // Example error condition: if accessing old.Value causes a connection attempt and that fails.
            }
        }
    }
}

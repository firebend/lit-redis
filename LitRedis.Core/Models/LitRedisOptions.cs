using System;

namespace LitRedis.Core.Models
{
    public class LitRedisOptions
    {
        public string ConnectionString { get; set; }

        public int RedisDeltaBackOffMilliseconds {get; set;} = 5_000;
        public int RedisAsyncTimeout {get; set;} = 10_000;
        public int RedisConnectTimeout {get; set;} = 10_000;
        public int RedisSyncTimeout {get; set;} = 10_000;

        // In general, let StackExchange.Redis handle most reconnects,
        // so limit the frequency of how often this will actually reconnect.
        public TimeSpan ReconnectMinFrequency { get; set; } = TimeSpan.FromSeconds(60);

        // if errors continue for longer than the below threshold, then the
        // multiplexer seems to not be reconnecting, so re-create the multiplexer
        public TimeSpan ReconnectErrorThreshold { get; set; } = TimeSpan.FromSeconds(30);
    }
}

using System.Text.Json;
using LitRedis.Core.Interfaces;

namespace LitRedis.Core.Implementations;

public class DefaultLitRedisSystemTextJsonOptionsProvider(JsonSerializerOptions options)
    : ILitRedisSystemTextJsonOptionsProvider
{
    public JsonSerializerOptions GetOptions() => options;
}

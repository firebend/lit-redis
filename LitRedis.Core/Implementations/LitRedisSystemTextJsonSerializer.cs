using System.Text.Json;
using LitRedis.Core.Interfaces;

namespace LitRedis.Core.Implementations;

public class LitRedisSystemTextJsonSerializer : ILitRedisJsonSerializer
{
    private readonly ILitRedisSystemTextJsonOptionsProvider _optionsProvider;

    public LitRedisSystemTextJsonSerializer(ILitRedisSystemTextJsonOptionsProvider optionsProvider)
    {
        _optionsProvider = optionsProvider;
    }

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _optionsProvider.GetOptions());

    public T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, _optionsProvider.GetOptions());
}

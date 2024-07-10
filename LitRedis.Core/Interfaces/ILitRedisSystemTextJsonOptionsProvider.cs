using System.Text.Json;

namespace LitRedis.Core.Interfaces;

public interface ILitRedisSystemTextJsonOptionsProvider
{
    JsonSerializerOptions GetOptions();
}

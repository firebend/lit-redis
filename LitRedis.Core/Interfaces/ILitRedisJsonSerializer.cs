namespace LitRedis.Core.Interfaces;

public interface ILitRedisJsonSerializer
{
    string Serialize<T>(T value);

    T Deserialize<T>(string value);
}

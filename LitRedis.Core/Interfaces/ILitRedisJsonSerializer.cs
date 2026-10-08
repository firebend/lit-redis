namespace LitRedis.Core.Interfaces;

public interface ILitRedisJsonSerializer
{
    public string Serialize<T>(T value);

    public T Deserialize<T>(string value);
}

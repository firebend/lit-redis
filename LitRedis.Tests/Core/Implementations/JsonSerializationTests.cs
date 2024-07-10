using System.Drawing;
using System.Dynamic;
using System.Text.Json;
using FluentAssertions;
using LitRedis.Core.Builders;
using LitRedis.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LitRedis.Tests.Core.Implementations;

internal class SampleClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public ExpandoObject Custom { get; set; }
}

[TestClass]
public class JsonSerializationTests
{
    [TestMethod]
    public void Json_Serializer_Should_Handle_Expando()
    {
        dynamic custom = new ExpandoObject();
        custom.Color = "Red";
        custom.Size = "Large";

        var sample = new SampleClass
        {
            Name = "John Doe",
            Age = 30,
            Custom = custom
        };

        var sp = new LitRedisServiceCollectionBuilder(new ServiceCollection()).ServiceCollection.BuildServiceProvider();
        var options = sp.GetRequiredService<ILitRedisSystemTextJsonOptionsProvider>().GetOptions();
        var serialized = JsonSerializer.Serialize(sample, options);
        var deserialized = JsonSerializer.Deserialize<SampleClass>(serialized, options);
        var serializedAgain = JsonSerializer.Serialize(deserialized, options);
        serializedAgain.Should()
            .Be("{\"name\":\"John Doe\",\"age\":30,\"custom\":{\"Color\":\"Red\",\"Size\":\"Large\"}}");
        dynamic c = deserialized.Custom;
        string color = c.Color.ToString();
        color.Should().Be("Red");
    }
}

using Halifax.Core.Helpers;

namespace Halifax.Core.Tests.Helpers;

public class JsonTests
{
    private record TestModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    [Test]
    public void Serialize_UsesCamelCase()
    {
        var model = new TestModel { Name = "John", Age = 30 };
        var json = Json.Serialize(model);

        Assert.That(json, Does.Contain("\"name\""));
        Assert.That(json, Does.Contain("\"age\""));
    }

    [Test]
    public void Deserialize_CaseInsensitive()
    {
        var json = """{"Name":"Jane","Age":25}""";
        var model = Json.Deserialize<TestModel>(json);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Jane"));
        Assert.That(model.Age, Is.EqualTo(25));
    }

    [Test]
    public void TryDeserialize_ValidJson_ReturnsTrue()
    {
        var success = Json.TryDeserialize<TestModel>("""{"name":"Test","age":1}""", out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void TryDeserialize_InvalidJson_ReturnsFalse()
    {
        var success = Json.TryDeserialize<TestModel>("not json", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void RoundTrip_PreservesData()
    {
        var original = new TestModel { Name = "RoundTrip", Age = 42 };
        var json = Json.Serialize(original);
        var deserialized = Json.Deserialize<TestModel>(json!);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Name, Is.EqualTo(original.Name));
        Assert.That(deserialized.Age, Is.EqualTo(original.Age));
    }

    [Test]
    public void Serialize_Null_ReturnsNull()
    {
        var json = Json.Serialize<TestModel>(null!);
        Assert.That(json, Is.Null);
    }
}

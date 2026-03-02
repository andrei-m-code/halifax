namespace Halifax.Core.Tests;

public class EnvTests
{
    [SetUp]
    public void SetUp()
    {
        // Clear any cached sections by using a fresh type each test
        Environment.SetEnvironmentVariable("EnvTestConfig__Name", null);
        Environment.SetEnvironmentVariable("EnvTestConfig__Port", null);
        Environment.SetEnvironmentVariable("EnvTestConfig__Enabled", null);
    }

    [Test]
    public void Load_MissingFileWithSwallowErrors_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Env.Load("nonexistent.env", swallowErrors: true));
    }

    [Test]
    public void Load_MissingFileWithoutSwallowErrors_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => Env.Load("nonexistent.env", swallowErrors: false));
    }

    [Test]
    public void GetSection_ReadsEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("SimpleConfig__value", "hello");

        var section = Env.GetSection<SimpleConfig>();

        Assert.That(section.Value, Is.EqualTo("hello"));

        Environment.SetEnvironmentVariable("SimpleConfig__value", null);
    }

    [Test]
    public void GetSection_ReturnsDefaultForMissingVars()
    {
        var section = Env.GetSection<DefaultConfig>();

        Assert.That(section.Name, Is.EqualTo("default"));
    }

    public class SimpleConfig(string? value = null)
    {
        public string? Value { get; } = value;
    }

    public class DefaultConfig(string name = "default")
    {
        public string Name { get; } = name;
    }
}

using Halifax.Core.Extensions;

namespace Halifax.Core.Tests.Extensions;

public class StringExtensionsTests
{
    [Test]
    public void IsEmail_ValidEmail_ReturnsTrue()
    {
        Assert.That("test@example.com".IsEmail(), Is.True);
    }

    [Test]
    public void IsEmail_InvalidEmail_ReturnsFalse()
    {
        Assert.That("not-an-email".IsEmail(), Is.False);
    }

    [Test]
    public void IsEmail_Null_ReturnsFalse()
    {
        Assert.That(((string?)null).IsEmail(), Is.False);
    }

    [Test]
    public void IsUrl_ValidUrl_ReturnsTrue()
    {
        Assert.That("https://example.com".IsUrl(), Is.True);
    }

    [Test]
    public void IsUrl_InvalidUrl_ReturnsFalse()
    {
        Assert.That("not-a-url".IsUrl(), Is.False);
    }

    [Test]
    public void ParseConnectionString_ParsesCorrectly()
    {
        var result = "Server=localhost;Database=test;User=admin".ParseConnectionString();

        Assert.That(result["Server"], Is.EqualTo("localhost"));
        Assert.That(result["Database"], Is.EqualTo("test"));
        Assert.That(result["User"], Is.EqualTo("admin"));
    }

    [Test]
    public void ParseConnectionString_CaseInsensitive()
    {
        var result = "Server=localhost".ParseConnectionString();
        Assert.That(result["server"], Is.EqualTo("localhost"));
    }

    [Test]
    public void CapitalizeWords_CapitalizesEachWord()
    {
        Assert.That("hello world".CapitalizeWords(), Is.EqualTo("Hello World"));
    }

    [Test]
    public void CapitalizeWords_SingleWord()
    {
        Assert.That("hello".CapitalizeWords(), Is.EqualTo("Hello"));
    }
}

using Halifax.Domain.Exceptions;

namespace Halifax.Core.Tests;

public class GuardTests
{
    [Test]
    public void NotNull_WithValue_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.NotNull("hello", "arg"));
    }

    [Test]
    public void NotNull_WithNull_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.NotNull(null, "arg"));
    }

    [Test]
    public void NotNull_WithCustomMessage_ThrowsWithMessage()
    {
        var ex = Assert.Throws<HalifaxException>(() => Guard.NotNull(null, "arg", "custom msg"));
        Assert.That(ex!.Message, Is.EqualTo("custom msg"));
    }

    [Test]
    public void NotNullOrWhiteSpace_WithValue_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.NotNullOrWhiteSpace("hello", "arg"));
    }

    [Test]
    public void NotNullOrWhiteSpace_WithEmpty_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.NotNullOrWhiteSpace("", "arg"));
    }

    [Test]
    public void NotNullOrWhiteSpace_WithWhiteSpace_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.NotNullOrWhiteSpace("   ", "arg"));
    }

    [Test]
    public void Email_ValidEmail_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.Email("test@example.com"));
    }

    [Test]
    public void Email_InvalidEmail_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Email("not-an-email"));
    }

    [Test]
    public void Url_ValidUrl_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.Url("https://example.com", "url"));
    }

    [Test]
    public void Url_InvalidUrl_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Url("not-a-url", "url"));
    }

    [Test]
    public void Length_WithinBounds_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.Length("hello", "arg", lower: 3, upper: 10));
    }

    [Test]
    public void Length_TooShort_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Length("hi", "arg", lower: 5));
    }

    [Test]
    public void Length_TooLong_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Length("hello world", "arg", upper: 5));
    }

    [Test]
    public void Range_WithinRange_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.Range(5, "arg", 1, 10));
    }

    [Test]
    public void Range_OutOfRange_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Range(15, "arg", 1, 10));
    }

    [Test]
    public void Ensure_TrueCondition_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.Ensure(true, "error"));
    }

    [Test]
    public void Ensure_FalseCondition_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Ensure(false, "error"));
    }

    [Test]
    public void NotEmptyList_WithItems_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.NotEmptyList(new[] { 1, 2, 3 }, "list"));
    }

    [Test]
    public void NotEmptyList_Empty_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.NotEmptyList(Array.Empty<int>(), "list"));
    }

    [Test]
    public void NotEmptyList_Null_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.NotEmptyList<int>(null, "list"));
    }

    [Test]
    public void Color_ValidHex_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Guard.Color("#fff", "color"));
        Assert.DoesNotThrow(() => Guard.Color("#FF00AA", "color"));
    }

    [Test]
    public void Color_InvalidHex_Throws()
    {
        Assert.Throws<HalifaxException>(() => Guard.Color("red", "color"));
        Assert.Throws<HalifaxException>(() => Guard.Color("#gggggg", "color"));
    }
}

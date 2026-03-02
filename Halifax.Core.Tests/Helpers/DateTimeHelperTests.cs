using Halifax.Core.Helpers;
using Halifax.Domain.Exceptions;

namespace Halifax.Core.Tests.Helpers;

public class DateTimeHelperTests
{
    [Test]
    public void ValidateRange_ValidRange_DoesNotThrow()
    {
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 12, 31);

        Assert.DoesNotThrow(() => DateTimeHelper.ValidateRange(from, to));
    }

    [Test]
    public void ValidateRange_InvalidRange_Throws()
    {
        var from = new DateTime(2024, 12, 31);
        var to = new DateTime(2024, 1, 1);

        Assert.Throws<HalifaxException>(() => DateTimeHelper.ValidateRange(from, to));
    }

    [Test]
    public void ValidateRange_NullValues_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => DateTimeHelper.ValidateRange(null, null));
        Assert.DoesNotThrow(() => DateTimeHelper.ValidateRange(null, DateTime.Now));
        Assert.DoesNotThrow(() => DateTimeHelper.ValidateRange(DateTime.Now, null));
    }

    [Test]
    public void IsIn_WithinRange_ReturnsTrue()
    {
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 12, 31);
        var point = new DateTime(2024, 6, 15);

        Assert.That(DateTimeHelper.IsIn(from, to, point), Is.True);
    }

    [Test]
    public void IsIn_OutsideRange_ReturnsFalse()
    {
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 12, 31);
        var point = new DateTime(2025, 6, 15);

        Assert.That(DateTimeHelper.IsIn(from, to, point), Is.False);
    }

    [Test]
    public void IsIn_NullBounds_ReturnsTrue()
    {
        var point = new DateTime(2024, 6, 15);

        Assert.That(DateTimeHelper.IsIn(null, null, point), Is.True);
    }

    [Test]
    public void ToIsoFormat_ReturnsIsoString()
    {
        DateTime? date = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = date.ToIsoFormat();

        Assert.That(result, Is.EqualTo("2024-03-15T10:30:00Z"));
    }

    [Test]
    public void ToIsoFormat_Null_ReturnsNull()
    {
        DateTime? date = null;
        Assert.That(date.ToIsoFormat(), Is.Null);
    }
}

using System.Collections.Concurrent;
using Halifax.Core.Helpers;

namespace Halifax.Core.Tests.Helpers;

public class ShortIdTests
{
    [Test]
    public void Create_DefaultsToSevenLettersAndDigits()
    {
        var id = ShortId.Create();

        Assert.That(id, Has.Length.EqualTo(7));
        Assert.That(id, Does.Match("^[a-zA-Z0-9]+$"));
    }

    [Test]
    public void Create_KeepsTheAlphabet()
    {
        // The same alphabet as earlier versions: no 'i' and no 'Z'.
        var all = string.Concat(Enumerable.Range(0, 2000).Select(_ => ShortId.Create(length: 20)));

        Assert.That(all, Does.Not.Contain("i").And.Not.Contain("Z"));
        Assert.That(all.Distinct().Count(), Is.EqualTo(25 + 25 + 10));
    }

    [Test]
    public void Create_LettersOnly()
    {
        var all = string.Concat(Enumerable.Range(0, 500).Select(_ => ShortId.Create(useNumbers: false, length: 20)));

        Assert.That(all, Does.Match("^[a-zA-Z]+$"));
    }

    [Test]
    public void Create_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => ShortId.Create(length: 6));
        Assert.That(ShortId.Create(length: 12), Has.Length.EqualTo(12));
    }

    [Test]
    public void Create_IsSafeToCallConcurrently()
    {
        var ids = new ConcurrentBag<string>();

        Parallel.For(0, 50_000, _ => ids.Add(ShortId.Create(length: 12)));

        // 60^12 possibilities: any duplicate here means the generator was corrupted by concurrent use.
        Assert.That(ids.Distinct().Count(), Is.EqualTo(50_000));
    }
}

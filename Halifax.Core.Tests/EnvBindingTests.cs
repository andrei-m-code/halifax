using System.Globalization;

namespace Halifax.Core.Tests;

/// <summary>
/// Settings binding. Env caches one instance per type, so every test binds its own type.
/// </summary>
[NonParallelizable]
public class EnvBindingTests
{
    private readonly List<string> variables = [];

    private void Set(string name, string? value)
    {
        Environment.SetEnvironmentVariable(name, value);
        variables.Add(name);
    }

    [TearDown]
    public void ClearVariables()
    {
        variables.ForEach(v => Environment.SetEnvironmentVariable(v, null));
        variables.Clear();
    }

    public enum Mode { Off, Fast, Safe }

    public record AllTypes(
        bool Flag, char Letter, byte B, sbyte Sb, short S, ushort Us, int I, uint Ui, long L, ulong Ul,
        float F, double D, decimal M, Guid G, DateTime When, DateTimeOffset At, DateOnly Day, TimeOnly Time,
        TimeSpan Span, Mode Mode, bool? MaybeFlag, int? MaybeInt, decimal? MaybeMoney, Mode? MaybeMode);

    [Test]
    public void ConstructorParameters_BindEverySupportedType()
    {
        var id = Guid.NewGuid();
        Set("AllTypes__Flag", "true");
        Set("AllTypes__Letter", "x");
        Set("AllTypes__B", "200");
        Set("AllTypes__Sb", "-5");
        Set("AllTypes__S", "-300");
        Set("AllTypes__Us", "60000");
        Set("AllTypes__I", "120");
        Set("AllTypes__Ui", "4000000000");
        Set("AllTypes__L", "9000000000");
        Set("AllTypes__Ul", "18000000000000000000");
        Set("AllTypes__F", "1.5");
        Set("AllTypes__D", "2.25");
        Set("AllTypes__M", "19.99");
        Set("AllTypes__G", id.ToString());
        Set("AllTypes__When", "2026-11-24T09:30:00");
        Set("AllTypes__At", "2026-11-24T09:30:00-05:00");
        Set("AllTypes__Day", "2026-11-24");
        Set("AllTypes__Time", "09:30");
        Set("AllTypes__Span", "00:02:00");
        Set("AllTypes__Mode", "safe");
        Set("AllTypes__MaybeFlag", "0");
        Set("AllTypes__MaybeInt", "7");
        Set("AllTypes__MaybeMoney", "0.5");
        Set("AllTypes__MaybeMode", "1");

        var s = Env.GetSection<AllTypes>();

        Assert.That(s, Is.EqualTo(new AllTypes(
            true, 'x', 200, -5, -300, 60000, 120, 4000000000, 9000000000, 18000000000000000000,
            1.5f, 2.25, 19.99m, id, new DateTime(2026, 11, 24, 9, 30, 0),
            new DateTimeOffset(2026, 11, 24, 9, 30, 0, TimeSpan.FromHours(-5)), new DateOnly(2026, 11, 24),
            new TimeOnly(9, 30), TimeSpan.FromMinutes(2), Mode.Safe, false, 7, 0.5m, Mode.Fast)));
    }

    public class PropertySettings
    {
        public bool Enabled { get; set; }
        public decimal Rate { get; set; } = 1m;
        public int? Limit { get; set; }
        public Mode Mode { get; set; } = Mode.Off;
        public DateOnly? Start { get; set; }
        public string Name { get; set; } = "default";
        public int Port { get; set; } = 80;
        public List<string> Unsupported { get; set; } = ["kept"];
    }

    [Test]
    public void SettableProperties_BindEverySupportedType()
    {
        Set("PropertySettings__Enabled", "True");
        Set("PropertySettings__Rate", "2.5");
        Set("PropertySettings__Limit", "10");
        Set("PropertySettings__Mode", "Fast");
        Set("PropertySettings__Start", "2026-12-01");
        Set("PropertySettings__Unsupported", "ignored");

        var s = Env.GetSection<PropertySettings>();

        Assert.That(s.Enabled, Is.True);
        Assert.That(s.Rate, Is.EqualTo(2.5m));
        Assert.That(s.Limit, Is.EqualTo(10));
        Assert.That(s.Mode, Is.EqualTo(Mode.Fast));
        Assert.That(s.Start, Is.EqualTo(new DateOnly(2026, 12, 1)));
        Assert.That(s.Name, Is.EqualTo("default"));
        Assert.That(s.Port, Is.EqualTo(80));
        Assert.That(s.Unsupported, Is.EqualTo(new[] { "kept" }));
    }

    public class EmptyValues
    {
        public int Port { get; set; } = 80;
        public string Name { get; set; } = "default";
    }

    [Test]
    public void EmptyValue_KeepsTheDefaultForNonStrings()
    {
        Set("EmptyValues__Port", " ");
        Set("EmptyValues__Name", "");

        var s = Env.GetSection<EmptyValues>();

        Assert.That(s.Port, Is.EqualTo(80));
        Assert.That(s.Name, Is.Empty);
    }

    public record CultureSettings(decimal Rate, double Ratio, DateTime When);

    [Test]
    public void Values_AreParsedWithTheInvariantCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Set("CultureSettings__Rate", "1.5");
            Set("CultureSettings__Ratio", "0.25");
            Set("CultureSettings__When", "11/24/2026");

            var s = Env.GetSection<CultureSettings>();

            Assert.That(s.Rate, Is.EqualTo(1.5m));
            Assert.That(s.Ratio, Is.EqualTo(0.25));
            Assert.That(s.When, Is.EqualTo(new DateTime(2026, 11, 24)));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    public record BadValue(int Port);

    [Test]
    public void BadValue_ThrowsNamingTheVariableButNotTheValue()
    {
        Set("BadValue__Port", "secret-looking-value");

        var e = Assert.Throws<FormatException>(() => Env.GetSection<BadValue>());

        Assert.That(e!.Message, Is.EqualTo("Environment variable BadValue__Port is not a valid Int32"));
        Assert.That(e.Message, Does.Not.Contain("secret-looking-value"));
    }

    public record BadBool(bool Flag);

    [Test]
    public void BadBool_Throws()
    {
        Set("BadBool__Flag", "yes please");

        Assert.Throws<FormatException>(() => Env.GetSection<BadBool>());
    }

    public record UnsupportedParameter(List<string> Items);

    [Test]
    public void UnsupportedConstructorParameter_WithAValue_StillThrowsNotSupported()
    {
        Set("UnsupportedParameter__Items", "a,b");

        Assert.Throws<NotSupportedException>(() => Env.GetSection<UnsupportedParameter>());
    }

    public record Defaults(int Port = 8080, bool Flag = true, string Name = "n");

    [Test]
    public void MissingValues_KeepConstructorDefaults()
    {
        var s = Env.GetSection<Defaults>();

        Assert.That(s, Is.EqualTo(new Defaults()));
    }
}

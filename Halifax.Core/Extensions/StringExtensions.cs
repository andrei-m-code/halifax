using System.Globalization;

namespace Halifax.Core.Extensions;

public static class StringExtensions
{
    public static bool IsEmail(this string? input)
    {
        return RegexConstants.EmailRegex().IsMatch(input ?? string.Empty);
    }

    public static bool IsUrl(this string input)
    {
        return Uri.TryCreate(input, UriKind.Absolute, out _);
    }

    public static Dictionary<string, string> ParseConnectionString(this string connectionString)
    {
        return connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Split(['='], 2))
            .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
    }

    public static string CapitalizeWords(this string text)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }
}

using System.Globalization;

namespace Halifax.Core.Extensions;

/// <summary>
/// String extension methods for validation and formatting.
/// </summary>
public static class StringExtensions
{
    /// <summary>Checks whether the string is a valid email address.</summary>
    public static bool IsEmail(this string? input)
    {
        return RegexConstants.EmailRegex().IsMatch(input ?? string.Empty);
    }

    /// <summary>Checks whether the string is a valid absolute URL.</summary>
    public static bool IsUrl(this string input)
    {
        return Uri.TryCreate(input, UriKind.Absolute, out _);
    }

    /// <summary>Parses a semicolon-delimited connection string into a key-value dictionary.</summary>
    public static Dictionary<string, string> ParseConnectionString(this string connectionString)
    {
        return connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Split(['='], 2))
            .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>Capitalizes the first letter of each word using invariant culture.</summary>
    public static string CapitalizeWords(this string text)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }
}

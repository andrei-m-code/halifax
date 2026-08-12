using System.Globalization;

namespace Halifax.Core.Extensions;

/// <summary>
/// String extension methods for validation and formatting.
/// </summary>
public static class StringExtensions
{
    /// <summary>Checks whether the string is a valid email address.</summary>
    /// <param name="input">The string to test; <see langword="null"/> is treated as empty and returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the value matches the email pattern; otherwise <see langword="false"/>.</returns>
    public static bool IsEmail(this string? input)
    {
        return RegexConstants.EmailRegex().IsMatch(input ?? string.Empty);
    }

    /// <summary>Checks whether the string is a valid absolute URL.</summary>
    /// <param name="input">The string to test.</param>
    /// <returns><see langword="true"/> when the value is a well-formed absolute URI; otherwise <see langword="false"/>.</returns>
    public static bool IsUrl(this string input)
    {
        return Uri.TryCreate(input, UriKind.Absolute, out _);
    }

    /// <summary>Parses a semicolon-delimited connection string into a key-value dictionary.</summary>
    /// <param name="connectionString">The connection string of <c>key=value</c> pairs separated by semicolons.</param>
    /// <returns>A case-insensitive dictionary of the parsed keys and values (trimmed of surrounding whitespace).</returns>
    /// <exception cref="ArgumentException">Thrown when a duplicate key is encountered.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown when a segment does not contain an <c>=</c> separator.</exception>
    public static Dictionary<string, string> ParseConnectionString(this string connectionString)
    {
        return connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Split(['='], 2))
            .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>Capitalizes the first letter of each word using invariant culture.</summary>
    /// <param name="text">The text to convert to title case.</param>
    /// <returns>The title-cased text.</returns>
    public static string CapitalizeWords(this string text)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }
}

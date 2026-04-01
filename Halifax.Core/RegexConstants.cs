using System.Text.RegularExpressions;

namespace Halifax.Core;

/// <summary>
/// Compiled regex patterns for common validations.
/// </summary>
public static partial class RegexConstants
{
    /// <summary>Email validation regex pattern.</summary>
    public const string Email = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";

    /// <summary>URL slug validation regex pattern.</summary>
    public const string Slug = @"[a-z0-9_-]";

    /// <summary>Returns a compiled email validation regex.</summary>
    [GeneratedRegex(Email, RegexOptions.IgnoreCase)]
    public static partial Regex EmailRegex();

    /// <summary>Returns a compiled slug validation regex.</summary>
    [GeneratedRegex(Slug)]
    public static partial Regex SlugRegex();
}

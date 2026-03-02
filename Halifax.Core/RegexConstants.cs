using System.Text.RegularExpressions;

namespace Halifax.Core;

public static partial class RegexConstants
{
    public const string Email = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
    public const string Slug = @"[a-z0-9_-]";

    [GeneratedRegex(Email, RegexOptions.IgnoreCase)]
    public static partial Regex EmailRegex();

    [GeneratedRegex(Slug)]
    public static partial Regex SlugRegex();
}

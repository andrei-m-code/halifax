using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Halifax.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsEmail(this string input)
        {
            return Regex.IsMatch(input ?? string.Empty, RegexConstants.Email, RegexOptions.IgnoreCase);
        }

        public static Dictionary<string, string> ParseConnectionString(this string connectionString)
        {
            return connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Split(new[] { '=' }, 2))
                .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
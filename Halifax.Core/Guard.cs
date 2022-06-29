using Halifax.Domain.Exceptions;
using Halifax.Core.Extensions;
using System.Text.RegularExpressions;

namespace Halifax.Core;

/// <summary>
/// Validation helper
/// </summary>
public static class Guard
{
    /// <summary>
    /// Make sure value is not null or white space
    /// </summary>
    public static void NotNullOrWhiteSpace(string input, string argument, string errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            var message = errorMessage ?? $"{argument} is required";
            throw new HalifaxException(message);
        }
    }

    /// <summary>
    /// Make sure value is a valid email
    /// </summary>
    public static void Email(string input, string argument = "Email")
    {
        Guard.Ensure(input.IsEmail(), $"{argument} ({input}) is invalid");
    }

    /// <summary>
    /// Check if value is a string with emails
    /// </summary>
    public static void StringWithEmails(string input, string argument = "Emails string", string separators = " ,;")
    {
        (input ?? string.Empty)
            .Split(separators.ToArray(), StringSplitOptions.RemoveEmptyEntries)
            .ToList().ForEach(email => Email(email, $"Email {email}"));
    }

    /// <summary>
    /// Check if value is URL
    /// </summary>
    public static void Url(string input, string argument)
    {
        const string pattern = @"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
        var valid = Regex.IsMatch(input, pattern, RegexOptions.Compiled | RegexOptions.Singleline);

        if (!valid)
        {
            throw new HalifaxException($"{argument} is not a well formatted url");
        }
    }

    /// <summary>
    /// Check if value length is within the boundaries
    /// </summary>
    public static void Length(string input, string argument, int? lower = null, int? upper = null)
    {
        if (lower.HasValue && input.Length < lower.Value)
        {
            throw new HalifaxException($"{argument} is too short, the length should be at least {lower.Value}");
        }

        if (upper.HasValue && input.Length > upper.Value)
        {
            throw new HalifaxException($"{argument} is too long, max allowed length is {upper.Value}");
        }
    }

    /// <summary>
    /// Check if value is in range
    /// </summary>
    public static void Range(int input, string argument, int from, int to)
    {
        if (input > to || input < from)
        {
            throw new HalifaxException($"{argument} should be between {from} and {to}");
        }
    }

    /// <summary>
    /// Check if value satisfies the condition
    /// </summary>
    public static void Ensure(bool condition, string errorMessage)
    {
        if (!condition)
        {
            throw new HalifaxException(errorMessage);
        }
    }

    /// <summary>
    /// Check if value is null
    /// </summary>
    public static void NotNull(object input, string argument, string errorMessage = null)
    {
        if (input == null)
        {
            var message = errorMessage ?? $"{argument} is required";
            throw new HalifaxException(message);
        }
    }

    /// <summary>
    /// Make sure - not an empty list
    /// </summary>
    public static void NotEmptyList<TItem>(IEnumerable<TItem> list, string argument)
    {
        Ensure(list?.Any() == true, $"{argument} can't be empty");
    }

    /// <summary>
    /// Validate the value is a color hash
    /// </summary>
    public static void Color(string value, string argument)
    {
        NotNull(value, argument);

        if (!Regex.IsMatch(value, "^#(?:[0-9a-fA-F]{3}){1,2}$"))
        {
            throw new HalifaxException($"{argument} is not a valid color");
        }
    }
}

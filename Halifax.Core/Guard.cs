using Halifax.Domain.Exceptions;
using Halifax.Core.Extensions;
using System.Text.RegularExpressions;

namespace Halifax.Core;

/// <summary>
/// Guard clauses for validating arguments and invariants. Every method throws a
/// <see cref="HalifaxException"/> when its condition is not met and otherwise returns silently,
/// so calls read as preconditions at the top of a method.
/// </summary>
/// <example>
/// <code>
/// Guard.NotNullOrWhiteSpace(name, nameof(name));
/// Guard.Range(age, nameof(age), 0, 120);
/// </code>
/// </example>
public static partial class Guard
{
    /// <summary>
    /// Ensures the supplied string is not <see langword="null"/>, empty, or white space.
    /// </summary>
    /// <param name="input">The value to validate.</param>
    /// <param name="argument">The argument name used to build the default error message.</param>
    /// <param name="errorMessage">An optional custom error message; defaults to "<paramref name="argument"/> is required".</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="input"/> is null, empty, or white space.</exception>
    public static void NotNullOrWhiteSpace(string input, string argument, string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            var message = errorMessage ?? $"{argument} is required";
            throw new HalifaxException(message);
        }
    }

    /// <summary>
    /// Ensures the supplied string is a well-formed email address.
    /// </summary>
    /// <param name="input">The value to validate.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="input"/> is not a valid email address.</exception>
    /// <seealso cref="StringExtensions.IsEmail"/>
    public static void Email(string input, string argument = "Email")
    {
        Guard.Ensure(input.IsEmail(), $"{argument} ({input}) is invalid");
    }

    /// <summary>
    /// Ensures every token in a delimited string is a valid email address.
    /// </summary>
    /// <param name="input">The delimited string of email addresses to validate.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <param name="separators">The set of characters used to split <paramref name="input"/> into individual addresses.</param>
    /// <exception cref="HalifaxException">Thrown when any token is not a valid email address.</exception>
    /// <remarks>A <see langword="null"/> <paramref name="input"/> is treated as an empty string and passes validation.</remarks>
    public static void StringWithEmails(string input, string argument = "Emails string", string separators = " ,;")
    {
        (input ?? string.Empty)
            .Split(separators.ToArray(), StringSplitOptions.RemoveEmptyEntries)
            .ToList().ForEach(email => Email(email, $"Email {email}"));
    }

    /// <summary>
    /// Ensures the supplied string is a well-formed HTTP or HTTPS URL.
    /// </summary>
    /// <param name="input">The value to validate.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="input"/> is not a well-formatted URL.</exception>
    public static void Url(string input, string argument)
    {
        var valid = UrlRegex().IsMatch(input);

        if (!valid)
        {
            throw new HalifaxException($"{argument} is not a well formatted url");
        }
    }

    /// <summary>
    /// Ensures the length of a string falls within the optional lower and upper bounds.
    /// </summary>
    /// <param name="input">The value whose length is validated.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <param name="lower">The inclusive minimum length, or <see langword="null"/> to skip the lower-bound check.</param>
    /// <param name="upper">The inclusive maximum length, or <see langword="null"/> to skip the upper-bound check.</param>
    /// <exception cref="HalifaxException">Thrown when the length is below <paramref name="lower"/> or above <paramref name="upper"/>.</exception>
    /// <remarks>Both bounds are inclusive, and a <see langword="null"/> bound leaves that side unconstrained.</remarks>
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
    /// Ensures an integer falls within an inclusive range.
    /// </summary>
    /// <param name="input">The value to validate.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <param name="from">The inclusive lower bound.</param>
    /// <param name="to">The inclusive upper bound.</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="input"/> is less than <paramref name="from"/> or greater than <paramref name="to"/>.</exception>
    /// <remarks>Both <paramref name="from"/> and <paramref name="to"/> are inclusive.</remarks>
    public static void Range(int input, string argument, int from, int to)
    {
        if (input > to || input < from)
        {
            throw new HalifaxException($"{argument} should be between {from} and {to}");
        }
    }

    /// <summary>
    /// Ensures a boolean condition holds, throwing with the supplied message otherwise.
    /// </summary>
    /// <param name="condition">The condition that must be <see langword="true"/>.</param>
    /// <param name="errorMessage">The error message used when the condition is <see langword="false"/>.</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="condition"/> is <see langword="false"/>.</exception>
    public static void Ensure(bool condition, string errorMessage)
    {
        if (!condition)
        {
            throw new HalifaxException(errorMessage);
        }
    }

    /// <summary>
    /// Ensures the supplied value is not <see langword="null"/>.
    /// </summary>
    /// <param name="input">The value to validate.</param>
    /// <param name="argument">The argument name used to build the default error message.</param>
    /// <param name="errorMessage">An optional custom error message; defaults to "<paramref name="argument"/> is required".</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static void NotNull(object? input, string argument, string? errorMessage = null)
    {
        if (input == null)
        {
            var message = errorMessage ?? $"{argument} is required";
            throw new HalifaxException(message);
        }
    }

    /// <summary>
    /// Ensures a sequence is not <see langword="null"/> and contains at least one item.
    /// </summary>
    /// <typeparam name="TItem">The element type of the sequence.</typeparam>
    /// <param name="list">The sequence to validate.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="list"/> is <see langword="null"/> or empty.</exception>
    public static void NotEmptyList<TItem>(IEnumerable<TItem>? list, string argument)
    {
        Ensure(list?.Any() == true, $"{argument} can't be empty");
    }

    /// <summary>
    /// Ensures the supplied value is a valid hexadecimal color code (e.g. <c>#fff</c> or <c>#ffffff</c>).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="argument">The argument name used to build the error message.</param>
    /// <exception cref="HalifaxException">Thrown when <paramref name="value"/> is <see langword="null"/> or not a valid color code.</exception>
    public static void Color(string value, string argument)
    {
        NotNull(value, argument);

        if (!ColorRegex().IsMatch(value))
        {
            throw new HalifaxException($"{argument} is not a valid color");
        }
    }

    [GeneratedRegex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", RegexOptions.Singleline)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}){1,2}$")]
    private static partial Regex ColorRegex();
}

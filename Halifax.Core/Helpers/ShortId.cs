using System.Security.Cryptography;

namespace Halifax.Core.Helpers;

/// <summary>
/// Generates random short identifier strings.
/// </summary>
public static class ShortId
{
    private const string bigs = "ABCDEFGHIJKLMNOPQRSTUVWXY";
    private const string smalls = "abcdefghjlkmnopqrstuvwxyz";
    private const string numbers = "0123456789";
    private static readonly string letters = $"{smalls}{bigs}";
    private static readonly string lettersAndNumbers = $"{smalls}{bigs}{numbers}";

    /// <summary>
    /// Creates a random short ID string drawn from mixed-case letters and, optionally, digits.
    /// </summary>
    /// <param name="useNumbers">Whether to include digits in the character pool.</param>
    /// <param name="length">The length of the generated ID. Minimum is 7.</param>
    /// <returns>A random identifier of the requested length.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="length"/> is less than 7.</exception>
    /// <remarks>
    /// Characters come from <see cref="RandomNumberGenerator"/>, so IDs are unpredictable and the method is safe to
    /// call concurrently. IDs are not guaranteed to be globally unique; use a <see cref="Guid"/> when uniqueness is
    /// required.
    /// </remarks>
    /// <example>
    /// <code>
    /// var id = ShortId.Create();            // e.g. "aB3kR9t"
    /// var letters = ShortId.Create(useNumbers: false, length: 10);
    /// </code>
    /// </example>
    public static string Create(bool useNumbers = true, int length = 7)
    {
        if (length < 7)
        {
            throw new ArgumentException($"The specified length of {length} is less than the lower limit of 7.");
        }

        // A cryptographic generator: thread-safe, and IDs used as codes or links cannot be predicted.
        var pool = useNumbers ? lettersAndNumbers : letters;
        return new string(RandomNumberGenerator.GetItems<char>(pool, length));
    }
}

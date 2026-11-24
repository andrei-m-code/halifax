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
    /// Draws from <see cref="Random.Shared"/>, so the method is safe to call concurrently. IDs are not
    /// guaranteed to be globally unique; use a <see cref="Guid"/> when uniqueness is required.
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

        // Random.Shared is thread-safe; a shared new Random() is not, and concurrent Next() calls
        // can corrupt it into returning the same values.
        var pool = useNumbers ? lettersAndNumbers : letters;
        var output = new char[length];
        for (var i = 0; i < length; i++)
        {
            output[i] = pool[Random.Shared.Next(pool.Length)];
        }

        return new string(output);
    }
}

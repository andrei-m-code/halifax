using System.Text;

namespace Halifax.Core.Helpers;

public static class ShortId
{
    private static readonly object root = new();
    private static readonly Random random = new();
    private const string bigs = "ABCDEFGHIJKLMNOPQRSTUVWXY";
    private const string smalls = "abcdefghjlkmnopqrstuvwxyz";
    private const string numbers = "0123456789";
    private static readonly string pool = $"{smalls}{bigs}";

    public static string Create(bool useNumbers = true, int length = 7)
    {
        if (length < 7)
        {
            throw new ArgumentException($"The specified length of {length} is less than the lower limit of 7.");
        }

        string characterPool;
        Random rand;

        lock (root)
        {
            characterPool = pool;
            rand = random;
        }

        var poolBuilder = new StringBuilder(characterPool);
        if (useNumbers)
        {
            poolBuilder.Append(numbers);
        }

        var currentPool = poolBuilder.ToString();

        var output = new char[length];
        for (var i = 0; i < length; i++)
        {
            var charIndex = rand.Next(0, currentPool.Length);
            output[i] = currentPool[charIndex];
        }

        return new string(output);
    }
}

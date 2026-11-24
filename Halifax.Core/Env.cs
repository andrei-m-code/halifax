using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Halifax.Core;

/// <summary>
/// Loads environment variables from a file and binds them to strongly-typed configuration sections.
/// </summary>
public static partial class Env
{
    private static readonly ConcurrentDictionary<Type, object> sections = [];
    private static readonly HashSet<Type> parsableTypes =
    [
        typeof(bool), typeof(char),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
        typeof(Guid),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan)
    ];

    /// <summary>
    /// Loads environment variables from a file (usually for local execution).
    /// </summary>
    /// <param name="envFilename">Path to a file with <c>Variable=Value</c> lines. Relative paths are searched for from the current directory upward. Blank lines and lines starting with <c>#</c> are ignored.</param>
    /// <param name="swallowErrors">When <see langword="true"/> (the default), file-not-found and parsing errors are ignored; when <see langword="false"/>, they are rethrown.</param>
    /// <exception cref="FileNotFoundException">Thrown when the file cannot be found and <paramref name="swallowErrors"/> is <see langword="false"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a line is malformed (missing <c>=</c> or an invalid variable name) and <paramref name="swallowErrors"/> is <see langword="false"/>.</exception>
    public static void Load(string envFilename = ".env", bool swallowErrors = true)
    {
        try
        {
            LoadVariables(envFilename);
        }
        catch
        {
            if (!swallowErrors)
            {
                throw;
            }
        }
    }

    private static void LoadVariables(string envFilename)
    {
        var filename = envFilename;

        if (!Path.IsPathFullyQualified(envFilename))
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !dir.GetFiles(envFilename).Any())
            {
                dir = dir.Parent;
            }

            if (dir != null)
            {
                filename = Path.Combine(dir.FullName, filename);
            }
            else
            {
                throw new FileNotFoundException("Couldn't find the file", envFilename);
            }
        }

        File.ReadAllLines(filename)
            .Select((line, index) => (Line: line.Trim(), Index: index))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Line))
            .Where(entry => !entry.Line.StartsWith('#'))
            .ToList()
            .ForEach(entry =>
            {
                var (line, index) = entry;
                var indexOfEquality = line.IndexOf('=');
                if (indexOfEquality == -1)
                {
                    throw new InvalidOperationException($"Equality sign is missing on line {index}");
                }

                var name = line[..indexOfEquality].Trim();
                var value = line[(indexOfEquality + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(name) || !EnvVarNameRegex().IsMatch(name))
                {
                    throw new InvalidOperationException($"Bad environment variable name {name} on line {index}");
                }

                Environment.SetEnvironmentVariable(name, value);
            });
    }

    /// <summary>
    /// Builds a strongly-typed configuration object from environment variables, binding each member
    /// from a variable named <c>{section}__{member}</c>. The result is cached per type.
    /// </summary>
    /// <typeparam name="TSection">The configuration type to construct and populate.</typeparam>
    /// <param name="section">The section prefix for variable names; defaults to the type name of <typeparamref name="TSection"/>.</param>
    /// <returns>The populated configuration instance (cached after the first call).</returns>
    /// <exception cref="NotSupportedException">Thrown when a constructor parameter whose variable is set has a type that cannot be parsed from a string.</exception>
    /// <exception cref="FormatException">Thrown when a variable's value cannot be parsed as its member's type. The message names the variable but not its value.</exception>
    /// <remarks>
    /// <para>The constructor with the most parameters is used; any remaining settable properties of a supported type
    /// are then bound. A member whose variable is missing (or empty, for non-string members) keeps its default.
    /// Values are read via <see cref="Environment.GetEnvironmentVariable(string)"/>, so call <see cref="Load"/> first
    /// when using an env file.</para>
    /// <para>Supported types: <see cref="string"/>, <see cref="bool"/> (<c>true</c>/<c>false</c>/<c>1</c>/<c>0</c>),
    /// <see cref="char"/>, every integer type, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>,
    /// <see cref="Guid"/>, <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="DateOnly"/>,
    /// <see cref="TimeOnly"/>, <see cref="TimeSpan"/>, enums (by name, case-insensitive, or number) and the nullable
    /// versions of all of them. Values are parsed with <see cref="CultureInfo.InvariantCulture"/>.</para>
    /// </remarks>
    public static TSection GetSection<TSection>(string? section = null)
    {
        var configType = typeof(TSection);
        if (sections.TryGetValue(configType, out var value))
        {
            return (TSection)value;
        }

        section ??= typeof(TSection).Name;

        var constructor = typeof(TSection).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var parameters = constructor
            .GetParameters()
            .Select(parameter => GetConstructorParameter(section, parameter))
            .ToArray();

        var instance = (TSection?)Activator.CreateInstance(typeof(TSection), parameters);

        if (instance != null)
        {
            var ctorParameterNames = constructor.GetParameters().Select(p => p.Name);

            instance.GetType()
                .GetProperties()
                .Where(p => !ctorParameterNames.Contains(p.Name))
                .Where(p => IsSupportedType(p.PropertyType))
                .Where(p => p.SetMethod != null)
                .ToList()
                .ForEach(property =>
                {
                    var parameter = GetPropertyParameter(section, instance, property);
                    property.SetValue(instance, parameter);
                });
        }

        return (TSection)sections.GetOrAdd(configType, instance!);
    }

    private static object? GetPropertyParameter(string section, object instance, PropertyInfo property)
    {
        var environmentKey = $"{section}__{property.Name}";
        return GetParameter(property.PropertyType, environmentKey, property.GetValue(instance));
    }

    private static object? GetConstructorParameter(string section, ParameterInfo parameter)
    {
        var environmentKey = $"{section}__{parameter.Name}";
        var val = Environment.GetEnvironmentVariable(environmentKey);
        var type = parameter.ParameterType;

        if (string.IsNullOrWhiteSpace(val))
        {
            return parameter.HasDefaultValue && parameter.DefaultValue != null
                ? parameter.DefaultValue
                : (type.IsValueType ? Activator.CreateInstance(type) : null);
        }

        return GetParameter(type, environmentKey);
    }

    private static object? GetParameter(Type type, string environmentKey, object? defaultValue = null)
    {
        var val = Environment.GetEnvironmentVariable(environmentKey)?.Trim();
        if (val == null)
        {
            return defaultValue;
        }

        if (type == typeof(string)) return val;
        if (val.Length == 0) return defaultValue;

        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (!IsParsable(target))
        {
            throw new NotSupportedException($"The configuration property type {type.Name} is not supported");
        }

        try
        {
            return Parse(target, val);
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
        {
            // The value is left out on purpose: settings often hold secrets.
            throw new FormatException($"Environment variable {environmentKey} is not a valid {target.Name}", e);
        }
    }

    private static object Parse(Type type, string val)
    {
        var culture = CultureInfo.InvariantCulture;

        if (type.IsEnum) return Enum.Parse(type, val, ignoreCase: true);
        if (type == typeof(bool)) return ParseBool(val);
        if (type == typeof(char)) return val.Length == 1 ? val[0] : throw new FormatException("Expected a single character");
        if (type == typeof(byte)) return byte.Parse(val, culture);
        if (type == typeof(sbyte)) return sbyte.Parse(val, culture);
        if (type == typeof(short)) return short.Parse(val, culture);
        if (type == typeof(ushort)) return ushort.Parse(val, culture);
        if (type == typeof(int)) return int.Parse(val, culture);
        if (type == typeof(uint)) return uint.Parse(val, culture);
        if (type == typeof(long)) return long.Parse(val, culture);
        if (type == typeof(ulong)) return ulong.Parse(val, culture);
        if (type == typeof(float)) return float.Parse(val, culture);
        if (type == typeof(double)) return double.Parse(val, culture);
        if (type == typeof(decimal)) return decimal.Parse(val, culture);
        if (type == typeof(Guid)) return Guid.Parse(val);
        if (type == typeof(DateTime)) return DateTime.Parse(val, culture);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(val, culture);
        if (type == typeof(DateOnly)) return DateOnly.Parse(val, culture);
        if (type == typeof(TimeOnly)) return TimeOnly.Parse(val, culture);
        return TimeSpan.Parse(val, culture);
    }

    private static bool ParseBool(string val)
    {
        if (bool.TryParse(val, out var flag)) return flag;

        return val switch
        {
            "1" => true,
            "0" => false,
            _ => throw new FormatException("Expected true, false, 1 or 0")
        };
    }

    private static bool IsParsable(Type type) => type.IsEnum || parsableTypes.Contains(type);

    private static bool IsSupportedType(Type type)
    {
        return type == typeof(string) || IsParsable(Nullable.GetUnderlyingType(type) ?? type);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_]*$", RegexOptions.Singleline)]
    private static partial Regex EnvVarNameRegex();
}

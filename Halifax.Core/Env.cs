using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Halifax.Core;

/// <summary>
/// Loads environment variables from a file and binds them to strongly-typed configuration sections.
/// </summary>
public static partial class Env
{
    private static readonly ConcurrentDictionary<Type, object> sections = [];
    private static readonly List<Type> supportedTypes =
    [
        typeof(Guid), typeof(Guid?),
        typeof(DateTime), typeof(DateTime?),
        typeof(TimeSpan), typeof(TimeSpan?)
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
    /// <exception cref="NotSupportedException">Thrown when a bound member has a type that cannot be parsed from a string.</exception>
    /// <remarks>
    /// The constructor with the most parameters is used; any remaining settable properties of a
    /// supported type are then bound. Values are read via <see cref="Environment.GetEnvironmentVariable(string)"/>,
    /// so call <see cref="Load"/> first when using an env file.
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
        if (type == typeof(byte) || type == typeof(byte?)) return byte.Parse(val);
        if (type == typeof(short) || type == typeof(short?)) return short.Parse(val);
        if (type == typeof(int) || type == typeof(int?)) return int.Parse(val);
        if (type == typeof(long) || type == typeof(long?)) return long.Parse(val);
        if (type == typeof(double) || type == typeof(double?)) return double.Parse(val);
        if (type == typeof(float) || type == typeof(float?)) return float.Parse(val);
        if (type == typeof(decimal) || type == typeof(decimal?)) return decimal.Parse(val);
        if (type == typeof(Guid) || type == typeof(Guid?)) return Guid.Parse(val);
        if (type == typeof(DateTime) || type == typeof(DateTime?)) return DateTime.Parse(val);
        if (type == typeof(TimeSpan) || type == typeof(TimeSpan?)) return TimeSpan.Parse(val);

        throw new NotSupportedException($"The configuration property type {type.Name} is not supported");
    }

    private static bool IsSupportedType(Type type)
    {
        return type == typeof(string) || type.IsPrimitive || supportedTypes.Contains(type);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_]*$", RegexOptions.Singleline)]
    private static partial Regex EnvVarNameRegex();
}

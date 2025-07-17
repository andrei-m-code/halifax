using System.Reflection;

namespace Halifax.Excel;

internal static class ObjectActivator
{
    public static TDestination Activate<TDestination>(Dictionary<string, object> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var targetType = typeof(TDestination);
        var comparer = StringComparer.OrdinalIgnoreCase;
        var dict = new Dictionary<string, object>(data, comparer);

        // Find the best matching constructor
        var constructors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Count(p => dict.ContainsKey(p.Name)))
            .ToList();

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            bool allMatched = true;
            var usedKeys = new HashSet<string>(comparer);

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (dict.TryGetValue(param.Name!, out var value))
                {
                    try
                    {
                        args[i] = ConvertValue(value, param.ParameterType);
                        usedKeys.Add(param.Name!);
                    }
                    catch
                    {
                        allMatched = false;
                        break;
                    }
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    allMatched = false;
                    break;
                }
            }

            if (!allMatched) continue;

            var instance = (TDestination)ctor.Invoke(args);

            SetPublicProperties(instance, dict, usedKeys);
            return instance;
        }

        throw new InvalidOperationException($"No suitable constructor found for type {targetType.Name}");
    }

    private static void SetPublicProperties<T>(T instance, Dictionary<string, object> dict, HashSet<string> usedKeys)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in props)
        {
            if (usedKeys.Contains(prop.Name))
                continue;

            if (dict.TryGetValue(prop.Name, out var value))
            {
                try
                {
                    var converted = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(instance, converted);
                }
                catch
                {
                    // Skip property if conversion fails
                }
            }
        }
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType.IsEnum && value is string s)
            return Enum.Parse(targetType, s, ignoreCase: true);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlying = Nullable.GetUnderlyingType(targetType)!;
            return ConvertValue(value, underlying);
        }

        return Convert.ChangeType(value, targetType);
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Halifax.Core.Helpers;

/// <summary>
/// Serialize or deserialize objects to and from JSON
/// </summary>
public static class Json
{
    static Json()
    {
        ConfigureOptions = options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new UniversalDateTimeConverter());
        };
    }

    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    /// <param name="obj">Object to serialize</param>
    /// <param name="indented">Write indented</param>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <returns>JSON representation of an object</returns>
    public static string Serialize<TObject>(TObject obj, bool indented = false) where TObject : class
    {
        var options = new JsonSerializerOptions();
        ConfigureOptions(options);
        options.WriteIndented = indented;

        return Serialize(obj, options);
    }

    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    /// <param name="obj">Object to serialize</param>
    /// <param name="options">Serialization options</param>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <returns>JSON representation of an object</returns>
    public static string Serialize<TObject>(TObject obj, JsonSerializerOptions options) where TObject : class
    {
        return obj != null
            ? JsonSerializer.Serialize(obj, options)
            : null;
    }
    
    /// <summary>
    /// Deserialize object from JSON
    /// </summary>
    /// <param name="jsonString">JSON string representation of an object</param>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <returns>An object that was serialized to JSON</returns>
    public static TObject Deserialize<TObject>(string jsonString) where TObject : class
    {
        var options = new JsonSerializerOptions();
        ConfigureOptions(options);

        return Deserialize<TObject>(jsonString, options);
    }
    
    /// <summary>
    /// Deserialize object from JSON
    /// </summary>
    /// <param name="jsonString">JSON string representation of an object</param>
    /// <param name="options">JSON serializer options</param>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <returns>An object that was serialized to JSON</returns>
    public static TObject Deserialize<TObject>(string jsonString, JsonSerializerOptions options) where TObject : class
    {
        return jsonString != null
            ? JsonSerializer.Deserialize<TObject>(jsonString, options)
            : null;
    }
    
    public static bool TryDeserialize<TObject>(string jsonString, out TObject result) where TObject : class
    {
        var options = new JsonSerializerOptions();
        ConfigureOptions(options);
        return TryDeserialize<TObject>(jsonString, options, out result);
    }
    
    public static bool TryDeserialize<TObject>(string jsonString, JsonSerializerOptions options, out TObject result) where TObject : class
    {
        try
        {
            result = Deserialize<TObject>(jsonString, options);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
    
    /// <summary>
    /// Deserialize object from JSON
    /// </summary>
    /// <param name="utf8Stream">UTF8 encoded json stream</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <returns>An object that was serialized to JSON</returns>
    public static async Task<Object> DeserializeAsync<TObject>(
        Stream utf8Stream, 
        CancellationToken cancellationToken = default) where TObject : class
    {
        var options = new JsonSerializerOptions();
        ConfigureOptions(options);

        return await DeserializeAsync<TObject>(utf8Stream, options, cancellationToken);
    }

    /// <summary>
    /// Deserialize object from JSON
    /// </summary>
    /// <param name="utf8Stream">UTF8 encoded json stream</param>
    /// <param name="options">JSON serializer options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <returns>An object that was serialized to JSON</returns>
    public static async Task<TObject> DeserializeAsync<TObject>(
        Stream utf8Stream, 
        JsonSerializerOptions options, 
        CancellationToken cancellationToken = default) where TObject : class
    {
        return utf8Stream == null
            ? null
            : await JsonSerializer.DeserializeAsync<TObject>(utf8Stream, options, cancellationToken);
    }
    
    /// <summary>
    /// Default serializer options factory method
    /// </summary>
    public static Action<JsonSerializerOptions> ConfigureOptions { get; set; }
}

public class UniversalDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime().ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
         writer.WriteStringValue(value.ToUniversalTime());
    }
}
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Halifax.Core.Helpers
{
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
                options.IgnoreNullValues = true;
                options.Converters.Add(new JsonStringEnumConverter());
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
            
            return obj != null
                ? JsonSerializer.Serialize(obj, options)
                : null;
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
            
            return jsonString != null
                ? JsonSerializer.Deserialize<TObject>(jsonString, options)
                : null;
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

        /// <summary>
        /// Default serializer options factory method
        /// </summary>
        public static Action<JsonSerializerOptions> ConfigureOptions { get; set; }
    }
}
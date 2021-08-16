using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Halifax.Core.Helpers
{
    /// <summary>
    /// Serialize or deserialize objects to and from JSON
    /// </summary>
    public static class Json
    {
        // TODO: We need to allow customizations to this later on.
        private static readonly JsonSerializerSettings settings = GetDefaultSettings();
        
        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <param name="formatting">Formatting (Indented or not)</param>
        /// <typeparam name="TObject">Object type</typeparam>
        /// <returns>JSON representation of an object</returns>
        public static string Encode<TObject>(TObject obj, Formatting formatting = Formatting.None) where TObject : class
        {
            return obj != null 
                ? JsonConvert.SerializeObject(obj, formatting, settings) 
                : null;
        }

        /// <summary>
        /// Deserialize object from JSON
        /// </summary>
        /// <param name="jsonString">JSON string representation of an object</param>
        /// <typeparam name="TObject">Object type</typeparam>
        /// <returns>An object that was serialized to JSON</returns>
        public static TObject Decode<TObject>(string jsonString) where TObject : class
        {
            return jsonString != null 
                ? JsonConvert.DeserializeObject<TObject>(jsonString, settings) 
                : null;
        }

        /// <summary>
        /// Get default Halifax json serializer settings
        /// </summary>
        /// <returns>Json serializer settings object</returns>
        public static JsonSerializerSettings GetDefaultSettings()
        {
            var result = new JsonSerializerSettings();
            result.Converters.Add(new StringEnumConverter());
            result.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return result;
        }
    }
}
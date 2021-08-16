using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Halifax.Core.Helpers
{
    public static class Json
    {
        // TODO: We need to allow customizations to this later on.
        private static readonly JsonSerializerSettings settings = GetDefaultSettings();
        
        public static string Serialize<TObject>(TObject obj, Formatting formatting = Formatting.None) where TObject : class
        {
            return obj != null 
                ? JsonConvert.SerializeObject(obj, formatting, settings) 
                : null;
        }

        public static TObject Deserialize<TObject>(string objString) where TObject : class
        {
            return objString != null 
                ? JsonConvert.DeserializeObject<TObject>(objString, settings) 
                : null;
        }

        public static JsonSerializerSettings GetDefaultSettings()
        {
            var result = new JsonSerializerSettings();
            result.Converters.Add(new StringEnumConverter());
            result.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return result;
        }
    }
}
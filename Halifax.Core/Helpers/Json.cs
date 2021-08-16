using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Halifax.Core.Helpers
{
    public static class Json
    {
        static Json()
        {
            DefaultSettings = new JsonSerializerSettings();
            DefaultSettings.Converters.Add(new StringEnumConverter());
            DefaultSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        }

        public static string Serialize<TObject>(TObject obj, Formatting formatting = Formatting.None) where TObject : class
        {
            return obj != null 
                ? JsonConvert.SerializeObject(obj, formatting, DefaultSettings) 
                : null;
        }

        public static TObject Deserialize<TObject>(string objString) where TObject : class
        {
            return objString != null 
                ? JsonConvert.DeserializeObject<TObject>(objString, DefaultSettings) 
                : null;
        }

        public static JsonSerializerSettings DefaultSettings
        {
            get;
            
            // TODO: We'll need to allow the customizations for it later
            private set;
        }
    }
}
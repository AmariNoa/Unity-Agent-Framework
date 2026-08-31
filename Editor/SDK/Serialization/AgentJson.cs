using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace com.amari_noa.unity_agent_framework.sdk.serialization
{
    /// <summary>
    /// Canonical JSON settings for the Unity side (Newtonsoft Json.NET).
    /// Rules (design doc section 114, decision 3): camelCase property names,
    /// enums as camelCase strings (numeric values rejected, unknown values throw),
    /// ISO 8601 dates, null properties omitted.
    /// The gateway applies the same rules with System.Text.Json.
    /// </summary>
    public static class AgentJson
    {
        public static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = DateParseHandling.DateTimeOffset,
            };
            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), allowIntegerValues: false));
            settings.Converters.Add(new AgentToolDescriptorJsonConverter());
            return settings;
        }

        private static readonly JsonSerializerSettings Settings = CreateSettings();

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        public static T Deserialize<T>(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
    }
}

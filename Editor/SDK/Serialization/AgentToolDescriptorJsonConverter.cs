using System;
using System.Collections.Generic;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace com.amari_noa.unity_agent_framework.sdk.serialization
{
    /// <summary>
    /// Serializes AgentToolDescriptor expanding InputSchemaJson / OutputSchemaJson
    /// (canonical JSON Schema text) into inline "inputSchema" / "outputSchema" objects,
    /// and folds them back into text on read. This expansion is part of the contract
    /// (design doc section 114, decision 3) and is covered by compatibility tests.
    /// </summary>
    public sealed class AgentToolDescriptorJsonConverter : JsonConverter<AgentToolDescriptor>
    {
        public override void WriteJson(JsonWriter writer, AgentToolDescriptor value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            WriteProperty(writer, serializer, "id", value.Id);
            WriteProperty(writer, serializer, "description", value.Description);
            WriteProperty(writer, serializer, "tags", value.Tags);
            WriteProperty(writer, serializer, "provider", value.Provider);
            WriteProperty(writer, serializer, "packageId", value.PackageId);
            WriteSchema(writer, "inputSchema", value.InputSchemaJson);
            WriteSchema(writer, "outputSchema", value.OutputSchemaJson);
            WriteProperty(writer, serializer, "executionContext", value.ExecutionContext);
            WriteProperty(writer, serializer, "mutation", value.Mutation);
            WriteProperty(writer, serializer, "supportsDryRun", value.SupportsDryRun);
            WriteProperty(writer, serializer, "requiresConfirm", value.RequiresConfirm);
            WriteProperty(writer, serializer, "undoable", value.Undoable);
            WriteProperty(writer, serializer, "capabilities", value.Capabilities);
            WriteProperty(writer, serializer, "exportPolicy", value.ExportPolicy);
            WriteProperty(writer, serializer, "externalAliases", value.ExternalAliases);
            writer.WriteEndObject();
        }

        public override AgentToolDescriptor ReadJson(
            JsonReader reader, Type objectType, AgentToolDescriptor existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new AgentToolDescriptor
            {
                Id = (string)obj["id"],
                Description = (string)obj["description"],
                Tags = ToObject<List<string>>(obj["tags"], serializer),
                Provider = (string)obj["provider"],
                PackageId = (string)obj["packageId"],
                InputSchemaJson = SchemaToText(obj["inputSchema"]),
                OutputSchemaJson = SchemaToText(obj["outputSchema"]),
                ExecutionContext = ToObject<AgentToolExecutionContext>(obj["executionContext"], serializer),
                Mutation = ToObject<AgentToolMutation>(obj["mutation"], serializer),
                SupportsDryRun = obj["supportsDryRun"]?.Value<bool>() ?? false,
                RequiresConfirm = obj["requiresConfirm"]?.Value<bool>() ?? false,
                Undoable = obj["undoable"]?.Value<bool>() ?? false,
                Capabilities = ToObject<List<string>>(obj["capabilities"], serializer),
                ExportPolicy = ToObject<AgentToolExportPolicy>(obj["exportPolicy"], serializer),
                ExternalAliases = ToObject<Dictionary<string, string>>(obj["externalAliases"], serializer),
            };
        }

        private static void WriteProperty(JsonWriter writer, JsonSerializer serializer, string name, object value)
        {
            if (value == null)
            {
                return;
            }

            writer.WritePropertyName(name);
            serializer.Serialize(writer, value);
        }

        private static void WriteSchema(JsonWriter writer, string name, string schemaJson)
        {
            if (schemaJson == null)
            {
                return;
            }

            writer.WritePropertyName(name);
            JToken.Parse(schemaJson).WriteTo(writer);
        }

        private static string SchemaToText(JToken token)
        {
            return token == null || token.Type == JTokenType.Null
                ? null
                : token.ToString(Formatting.None);
        }

        private static T ToObject<T>(JToken token, JsonSerializer serializer)
        {
            return token == null || token.Type == JTokenType.Null
                ? default
                : token.ToObject<T>(serializer);
        }
    }
}

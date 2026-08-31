using System;
using System.Collections.Generic;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using com.amari_noa.unity_agent_framework.sdk.serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.sdk.tests
{
    /// <summary>
    /// Serialization rule tests for the contract types (camelCase, enum strings,
    /// null omission, schema expansion, ISO 8601 round trip). The gateway repository
    /// holds the mirrored tests for System.Text.Json with the same golden strings.
    /// </summary>
    public class ContractSerializationTest
    {
        [Test]
        public void SerializesErrorWithCamelCaseNames()
        {
            var error = new AgentError
            {
                Code = AgentErrorCodes.ObjectNotFound,
                Message = "Object not found.",
                Provider = "core",
                Retryable = false,
                Details = new Dictionary<string, object> { { "canonicalUri", "unity://scene/1" } },
            };

            var json = AgentJson.Serialize(error);

            Assert.That(json, Is.EqualTo(
                "{\"code\":\"OBJECT_NOT_FOUND\",\"message\":\"Object not found.\",\"provider\":\"core\",\"retryable\":false,\"details\":{\"canonicalUri\":\"unity://scene/1\"}}"));
        }

        [Test]
        public void SerializesEnumAsCamelCaseString()
        {
            var json = AgentJson.Serialize(new MutationMetadata { Mutation = AgentToolMutation.Additive, DryRun = true });

            Assert.That(json, Is.EqualTo("{\"mutation\":\"additive\",\"dryRun\":true}"));
        }

        [Test]
        public void RejectsNumericEnumValue()
        {
            Assert.Throws<JsonSerializationException>(
                () => AgentJson.Deserialize<MutationMetadata>("{\"mutation\":2,\"dryRun\":false}"));
        }

        [Test]
        public void RejectsUnknownEnumValue()
        {
            Assert.Throws<JsonSerializationException>(
                () => AgentJson.Deserialize<MutationMetadata>("{\"mutation\":\"explosive\",\"dryRun\":false}"));
        }

        [Test]
        public void OmitsNullPropertiesFromResultEnvelope()
        {
            var result = new AgentResult<AgentObjectRef>
            {
                Success = true,
                Result = new AgentObjectRef { CanonicalUri = "unity://scene/1", Name = "Avatar" },
                ExecutionTimeMs = 12,
            };

            var json = AgentJson.Serialize(result);

            Assert.That(json, Is.EqualTo(
                "{\"success\":true,\"result\":{\"canonicalUri\":\"unity://scene/1\",\"name\":\"Avatar\"},\"executionTimeMs\":12}"));
        }

        [Test]
        public void ExpandsDescriptorSchemaToInlineObject()
        {
            var descriptor = new AgentToolDescriptor
            {
                Id = "unity.scene.list",
                Description = "List scenes.",
                Provider = "core",
                InputSchemaJson = "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\"}}}",
                ExecutionContext = AgentToolExecutionContext.MainThread,
                Mutation = AgentToolMutation.None,
                ExportPolicy = AgentToolExportPolicy.Standalone,
            };

            var json = AgentJson.Serialize(descriptor);

            StringAssert.Contains("\"inputSchema\":{\"type\":\"object\"", json);
            StringAssert.DoesNotContain("inputSchemaJson", json);

            var restored = AgentJson.Deserialize<AgentToolDescriptor>(json);
            Assert.That(restored.InputSchemaJson, Is.EqualTo(descriptor.InputSchemaJson));
            Assert.That(restored.Id, Is.EqualTo("unity.scene.list"));
            Assert.That(restored.ExecutionContext, Is.EqualTo(AgentToolExecutionContext.MainThread));
        }

        [Test]
        public void RoundTripsJobInfoWithIso8601Dates()
        {
            var info = new AgentJobInfo
            {
                JobId = "3b1c8d1e-0000-0000-0000-000000000001",
                ToolId = "unity.scene.list",
                Status = AgentJobStatus.Running,
                Progress = new AgentProgress { Ratio = 0.5f, Message = "half" },
                CreatedAt = new DateTimeOffset(2026, 8, 31, 1, 2, 3, TimeSpan.Zero),
                StartedAt = new DateTimeOffset(2026, 8, 31, 1, 2, 4, TimeSpan.Zero),
            };

            var json = AgentJson.Serialize(info);
            StringAssert.Contains("\"status\":\"running\"", json);
            StringAssert.Contains("2026-08-31T01:02:03+00:00", json);

            var restored = AgentJson.Deserialize<AgentJobInfo>(json);
            Assert.That(restored.CreatedAt, Is.EqualTo(info.CreatedAt));
            Assert.That(restored.StartedAt, Is.EqualTo(info.StartedAt));
            Assert.That(restored.CompletedAt, Is.Null);
            Assert.That(restored.Progress.Ratio, Is.EqualTo(0.5f));
        }
    }
}

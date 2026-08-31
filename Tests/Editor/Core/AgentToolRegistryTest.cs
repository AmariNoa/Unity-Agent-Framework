using System;
using com.amari_noa.unity_agent_framework.core.editor;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentToolRegistryTest
    {
        private static AgentToolDescriptor Descriptor(string id, string provider = "core")
        {
            return new AgentToolDescriptor
            {
                Id = id,
                Description = "test",
                Provider = provider,
                Mutation = AgentToolMutation.None,
                ExecutionContext = AgentToolExecutionContext.Background,
                ExportPolicy = AgentToolExportPolicy.Standalone,
            };
        }

        private static AgentResult<object> Handler(sdk.AgentToolInvocation invocation)
        {
            return new AgentResult<object> { Success = true };
        }

        [Test]
        public void RegistersAndListsToolsSortedById()
        {
            var registry = new AgentToolRegistry();
            registry.RegisterTool(Descriptor("unity.zzz.get"), Handler);
            registry.RegisterTool(Descriptor("unity.aaa.get"), Handler);

            var descriptors = registry.ListDescriptors();

            Assert.That(descriptors.Count, Is.EqualTo(2));
            Assert.That(descriptors[0].Id, Is.EqualTo("unity.aaa.get"));
            Assert.That(registry.Find("unity.zzz.get"), Is.Not.Null);
            Assert.That(registry.Find("unity.none.get"), Is.Null);
        }

        [Test]
        public void RejectsIdCollisionKeepingFirstRegistration()
        {
            var registry = new AgentToolRegistry();
            registry.RegisterTool(Descriptor("unity.scene.list", provider: "core"), Handler);

            LogAssert.Expect(LogType.Warning,
                "[UnityAgentFramework] Tool id collision for 'unity.scene.list': " +
                "already registered by provider 'core', rejected registration from provider 'other'.");
            registry.RegisterTool(Descriptor("unity.scene.list", provider: "other"), Handler);

            Assert.That(registry.ListDescriptors().Count, Is.EqualTo(1));
            Assert.That(registry.Find("unity.scene.list").Descriptor.Provider, Is.EqualTo("core"));
        }

        [Test]
        public void RejectsAliasCollision()
        {
            var registry = new AgentToolRegistry();
            var first = Descriptor("unity.scene.list");
            registry.RegisterTool(first, Handler);

            // Second tool explicitly claims the derived alias of the first.
            var second = Descriptor("unity.other.list");
            second.ExternalAliases = new System.Collections.Generic.Dictionary<string, string>
            {
                { "mcp", "unity_scene_list" },
            };

            LogAssert.Expect(LogType.Warning,
                "[UnityAgentFramework] Alias collision for 'unity_scene_list' " +
                "(tool 'unity.other.list' from provider 'core'): " +
                "already used by tool 'unity.scene.list'. Registration rejected.");
            registry.RegisterTool(second, Handler);

            Assert.That(registry.Find("unity.other.list"), Is.Null);
        }

        [Test]
        public void RejectsInvalidCanonicalIds()
        {
            var registry = new AgentToolRegistry();
            Assert.Throws<ArgumentException>(() => registry.RegisterTool(Descriptor("Unity.Scene.List"), Handler));
            Assert.Throws<ArgumentException>(() => registry.RegisterTool(Descriptor("unity.scene"), Handler));
            Assert.Throws<ArgumentException>(() => registry.RegisterTool(Descriptor("unity.scene-list.get"), Handler));
        }

        [Test]
        public void RegistersProviders()
        {
            var registry = new AgentToolRegistry();
            registry.RegisterProvider(new ProviderInfo { Id = "core", DisplayName = "Core" });
            registry.RegisterProvider(new ProviderInfo { Id = "core", DisplayName = "Core Updated" });

            var providers = registry.ListProviders();
            Assert.That(providers.Count, Is.EqualTo(1));
            Assert.That(providers[0].DisplayName, Is.EqualTo("Core Updated"));
        }
    }
}

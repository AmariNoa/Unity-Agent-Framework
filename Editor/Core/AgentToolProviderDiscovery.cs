#nullable enable

using System;
using com.amari_noa.unity_agent_framework.sdk;
using UnityEditor;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Discovers [AgentToolProvider] implementations via TypeCache and registers
    /// their tools. A failing provider is reported and skipped so one broken
    /// package cannot take the whole registry down.
    /// </summary>
    public static class AgentToolProviderDiscovery
    {
        public static void RegisterAll(AgentToolRegistry registry)
        {
            foreach (var type in TypeCache.GetTypesWithAttribute<AgentToolProviderAttribute>())
            {
                if (!typeof(IAgentToolProvider).IsAssignableFrom(type) || type.IsAbstract)
                {
                    Debug.LogWarning(
                        $"[UnityAgentFramework] '{type.FullName}' has [AgentToolProvider] but does not " +
                        "implement IAgentToolProvider; skipped.");
                    continue;
                }

                try
                {
                    var provider = (IAgentToolProvider)Activator.CreateInstance(type);
                    provider.RegisterTools(registry);
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[UnityAgentFramework] Tool provider '{type.FullName}' failed to register: {e.Message}");
                }
            }
        }
    }
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Central tool registry (design doc sections 3.2 and 114 decision 4).
    /// Canonical id and external alias collisions reject the latecomer with a
    /// console warning naming both providers. Reads take an immutable snapshot
    /// so HTTP handler threads never observe partial registrations.
    /// </summary>
    public sealed class AgentToolRegistry : IAgentToolRegistry
    {
        private static readonly Regex CanonicalIdPattern =
            new Regex("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*){2,3}$", RegexOptions.Compiled);

        private readonly object _gate = new object();
        private readonly Dictionary<string, RegisteredTool> _tools = new Dictionary<string, RegisteredTool>();
        private readonly Dictionary<string, string> _aliasToToolId = new Dictionary<string, string>();
        private readonly Dictionary<string, ProviderInfo> _providers = new Dictionary<string, ProviderInfo>();

        public sealed class RegisteredTool
        {
            public RegisteredTool(AgentToolDescriptor descriptor, AgentToolHandler handler)
            {
                Descriptor = descriptor;
                Handler = handler;
            }

            public AgentToolDescriptor Descriptor { get; }
            public AgentToolHandler Handler { get; }
        }

        public void RegisterProvider(ProviderInfo provider)
        {
            if (provider?.Id == null)
            {
                throw new ArgumentException("Provider id is required.", nameof(provider));
            }

            lock (_gate)
            {
                _providers[provider.Id] = provider;
            }
        }

        public void RegisterTool(AgentToolDescriptor descriptor, AgentToolHandler handler)
        {
            if (descriptor?.Id == null)
            {
                throw new ArgumentException("Tool id is required.", nameof(descriptor));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!CanonicalIdPattern.IsMatch(descriptor.Id))
            {
                throw new ArgumentException(
                    $"Tool id '{descriptor.Id}' does not follow the canonical naming rules " +
                    "(lowercase dot-separated, 3-4 segments).", nameof(descriptor));
            }

            lock (_gate)
            {
                if (_tools.TryGetValue(descriptor.Id, out var existing))
                {
                    Debug.LogWarning(
                        $"[UnityAgentFramework] Tool id collision for '{descriptor.Id}': " +
                        $"already registered by provider '{existing.Descriptor.Provider}', " +
                        $"rejected registration from provider '{descriptor.Provider}'.");
                    return;
                }

                foreach (var alias in EnumerateAliases(descriptor))
                {
                    if (_aliasToToolId.TryGetValue(alias, out var owner))
                    {
                        Debug.LogWarning(
                            $"[UnityAgentFramework] Alias collision for '{alias}' " +
                            $"(tool '{descriptor.Id}' from provider '{descriptor.Provider}'): " +
                            $"already used by tool '{owner}'. Registration rejected.");
                        return;
                    }
                }

                _tools.Add(descriptor.Id, new RegisteredTool(descriptor, handler));
                foreach (var alias in EnumerateAliases(descriptor))
                {
                    _aliasToToolId[alias] = descriptor.Id;
                }
            }
        }

        /// <summary>Default MCP/CLI alias: dots replaced by underscores (design doc decision 4).</summary>
        public static string DefaultExternalAlias(string canonicalId)
        {
            return canonicalId.Replace('.', '_');
        }

        private static IEnumerable<string> EnumerateAliases(AgentToolDescriptor descriptor)
        {
            yield return DefaultExternalAlias(descriptor.Id!);
            if (descriptor.ExternalAliases == null)
            {
                yield break;
            }

            foreach (var alias in descriptor.ExternalAliases.Values)
            {
                if (!string.IsNullOrEmpty(alias))
                {
                    yield return alias;
                }
            }
        }

        public RegisteredTool? Find(string toolId)
        {
            lock (_gate)
            {
                return _tools.TryGetValue(toolId, out var tool) ? tool : null;
            }
        }

        public List<AgentToolDescriptor> ListDescriptors()
        {
            lock (_gate)
            {
                return _tools.Values.Select(t => t.Descriptor).OrderBy(d => d.Id, StringComparer.Ordinal).ToList();
            }
        }

        public List<ProviderInfo> ListProviders()
        {
            lock (_gate)
            {
                return _providers.Values.OrderBy(p => p.Id, StringComparer.Ordinal).ToList();
            }
        }
    }
}

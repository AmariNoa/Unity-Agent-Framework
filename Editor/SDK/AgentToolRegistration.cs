#nullable enable

using System;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using com.amari_noa.unity_agent_framework.sdk.serialization;

namespace com.amari_noa.unity_agent_framework.sdk
{
    /// <summary>
    /// A single tool invocation as seen by a tool handler. Parameters are carried
    /// as canonical JSON text so the contract stays serializer-agnostic.
    /// </summary>
    public sealed class AgentToolInvocation
    {
        public AgentToolInvocation(string toolId, string? parametersJson, bool confirm, bool dryRun)
        {
            ToolId = toolId ?? throw new ArgumentNullException(nameof(toolId));
            ParametersJson = parametersJson;
            Confirm = confirm;
            DryRun = dryRun;
        }

        public string ToolId { get; }

        /// <summary>Raw JSON text of the "parameters" object. Null when absent.</summary>
        public string? ParametersJson { get; }

        /// <summary>Write convention field (design doc section 47).</summary>
        public bool Confirm { get; }

        /// <summary>Write convention field (design doc section 47).</summary>
        public bool DryRun { get; }

        /// <summary>Deserialize the parameters with the canonical JSON rules.</summary>
        public T? GetParameters<T>() where T : class
        {
            return ParametersJson == null ? null : AgentJson.Deserialize<T>(ParametersJson);
        }
    }

    /// <summary>Executes a tool. Runs on the thread selected by the descriptor's ExecutionContext.</summary>
    public delegate AgentResult<object> AgentToolHandler(AgentToolInvocation invocation);

    /// <summary>
    /// Registration surface handed to providers (design doc section 4).
    /// Implemented by the core; providers never talk to MCP directly.
    /// </summary>
    public interface IAgentToolRegistry
    {
        /// <summary>Register the provider metadata. Call once before registering tools.</summary>
        void RegisterProvider(ProviderInfo provider);

        /// <summary>
        /// Register one tool. Canonical id and alias collisions are rejected
        /// (first registration wins; design doc section 114, decision 4).
        /// </summary>
        void RegisterTool(AgentToolDescriptor descriptor, AgentToolHandler handler);
    }

    /// <summary>Implemented by classes that contribute tools to the registry.</summary>
    public interface IAgentToolProvider
    {
        void RegisterTools(IAgentToolRegistry registry);
    }

    /// <summary>
    /// Marks an IAgentToolProvider implementation (parameterless constructor required)
    /// for automatic discovery at editor load.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AgentToolProviderAttribute : Attribute
    {
    }
}

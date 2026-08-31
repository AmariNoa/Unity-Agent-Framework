using System.Collections.Generic;

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>Where the tool body runs.</summary>
    public enum AgentToolExecutionContext
    {
        MainThread,
        Background
    }

    /// <summary>Kind of mutation the tool performs. Drives permission level derivation.</summary>
    public enum AgentToolMutation
    {
        None,
        Additive,
        Overwrite,
        Destructive
    }

    /// <summary>Which surfaces the tool is exported to (design doc section 80).</summary>
    public enum AgentToolExportPolicy
    {
        Standalone,
        UnityOfficial,
        Both,
        InternalOnly
    }

    /// <summary>
    /// Canonical tool descriptor. Schemas are held as canonical JSON Schema text
    /// (draft 2020-12) and expanded to inline objects at delivery boundaries
    /// (MCP tools/list, HTTP tool discovery) by the per-side converters.
    /// </summary>
    public sealed class AgentToolDescriptor
    {
        /// <summary>Canonical tool id, e.g. unity.scene.list (naming rules: design doc decision 4).</summary>
        public string Id { get; set; }

        public string Description { get; set; }

        public List<string> Tags { get; set; }

        /// <summary>Provider id (ProviderInfo.Id).</summary>
        public string Provider { get; set; }

        /// <summary>Providing package id.</summary>
        public string PackageId { get; set; }

        /// <summary>Canonical JSON Schema (draft 2020-12) text for the input.</summary>
        public string InputSchemaJson { get; set; }

        /// <summary>Canonical JSON Schema (draft 2020-12) text for the output.</summary>
        public string OutputSchemaJson { get; set; }

        public AgentToolExecutionContext ExecutionContext { get; set; }

        public AgentToolMutation Mutation { get; set; }

        public bool SupportsDryRun { get; set; }

        public bool RequiresConfirm { get; set; }

        public bool Undoable { get; set; }

        public List<string> Capabilities { get; set; }

        public AgentToolExportPolicy ExportPolicy { get; set; }

        /// <summary>Alias per external surface: "mcp" / "unityPipeline" / "cli" -> alias name.</summary>
        public Dictionary<string, string> ExternalAliases { get; set; }
    }
}

#nullable enable

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>Protocol constants shared by the HTTP surface.</summary>
    public static class AgentProtocol
    {
        /// <summary>Wire protocol version verified during the gateway handshake.</summary>
        public const string Version = "1.0.0";
    }

    /// <summary>Payload of GET /api/status.</summary>
    public sealed class AgentStatusInfo
    {
        public AgentEditorState State { get; set; }
        public string? UnityVersion { get; set; }
        public string? FrameworkVersion { get; set; }
        public string? ProtocolVersion { get; set; }
    }
}

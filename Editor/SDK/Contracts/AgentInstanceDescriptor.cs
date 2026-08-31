#nullable enable

using System;

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>
    /// Instance descriptor payload (design doc section 51; superset of the official
    /// pipeline fields). Written by the core under Library/UnityAgent/ and read by
    /// the gateway for instance discovery, so it is part of the shared contract.
    /// </summary>
    public sealed class AgentInstanceDescriptor
    {
        public int Pid { get; set; }
        public int Port { get; set; }
        public string? ProjectPath { get; set; }
        public string? ProjectName { get; set; }
        public string? UnityVersion { get; set; }
        public string? Mode { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public string? FrameworkVersion { get; set; }
        public string? ProtocolVersion { get; set; }
        public string? Token { get; set; }
    }
}

#nullable enable

using System;

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>Job states matching the official pipeline (design doc section 54).</summary>
    public enum AgentJobStatus
    {
        Queued,
        Running,
        Completed,
        Failed,
        Canceled
    }

    /// <summary>Progress of a running invocation. Polled by clients.</summary>
    public sealed class AgentProgress
    {
        /// <summary>0.0 to 1.0. Null when a ratio cannot be computed.</summary>
        public float? Ratio { get; set; }

        /// <summary>Human readable description of the current step.</summary>
        public string? Message { get; set; }

        public int? CurrentStep { get; set; }

        public int? TotalSteps { get; set; }
    }

    /// <summary>
    /// Detached job info. The job result is returned separately as an AgentResult
    /// after completion and is not embedded here. Jobs do not survive domain reloads;
    /// lost jobs are reported as Failed with EDITOR_RELOADED.
    /// </summary>
    public sealed class AgentJobInfo
    {
        /// <summary>GUID string.</summary>
        public string? JobId { get; set; }

        public string? ToolId { get; set; }

        public AgentJobStatus Status { get; set; }

        /// <summary>Only while Running; omitted from JSON when null.</summary>
        public AgentProgress? Progress { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }
}

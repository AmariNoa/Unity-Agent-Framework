#nullable enable

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>
    /// Result envelope compatible with the official pipeline envelope
    /// (success / result / error / executionTimeMs).
    /// </summary>
    public sealed class AgentResult<T>
    {
        public bool Success { get; set; }

        /// <summary>Payload. Null on failure.</summary>
        public T? Result { get; set; }

        /// <summary>Error. Null on success.</summary>
        public AgentError? Error { get; set; }

        public long ExecutionTimeMs { get; set; }

        /// <summary>Pagination info. Only set for list / scan tools; omitted from JSON when null.</summary>
        public PageInfo? Page { get; set; }

        /// <summary>Mutation info. Only set for write tools; omitted from JSON when null.</summary>
        public MutationMetadata? Mutation { get; set; }
    }

    /// <summary>Standard pagination block (default limit 100, max 1000, default depth 1).</summary>
    public sealed class PageInfo
    {
        public int Offset { get; set; }
        public int Limit { get; set; }

        /// <summary>Total count. Null when computing it would be expensive.</summary>
        public long? Total { get; set; }

        public bool HasMore { get; set; }
    }
}

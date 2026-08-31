#nullable enable

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>Capability availability info (design doc section 14).</summary>
    public sealed class CapabilityInfo
    {
        /// <summary>Capability id, e.g. vrc.avatar.material.modify.</summary>
        public string? Capability { get; set; }

        public bool Available { get; set; }

        /// <summary>Providing provider id. Null when unavailable.</summary>
        public string? Provider { get; set; }

        public string? ProviderVersion { get; set; }
    }

    /// <summary>Provider metadata.</summary>
    public sealed class ProviderInfo
    {
        /// <summary>Example: core / vrchat / modular-avatar / lilycal-inventory.</summary>
        public string? Id { get; set; }

        public string? DisplayName { get; set; }

        public string? PackageId { get; set; }

        public string? PackageVersion { get; set; }

        /// <summary>Minimum required main package version (compatibility declaration).</summary>
        public string? MinimumFrameworkVersion { get; set; }
    }
}

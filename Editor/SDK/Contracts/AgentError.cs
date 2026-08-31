#nullable enable

using System.Collections.Generic;

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>
    /// Structured error returned to agents. Serializer-agnostic POCO (design doc section 114, decision 3).
    /// </summary>
    public sealed class AgentError
    {
        /// <summary>Machine readable code in SCREAMING_SNAKE_CASE. See <see cref="AgentErrorCodes"/>.</summary>
        public string? Code { get; set; }

        /// <summary>Human readable message (English).</summary>
        public string? Message { get; set; }

        /// <summary>Provider id the error originated from ("core" when raised by the core).</summary>
        public string? Provider { get; set; }

        /// <summary>Whether a retry may resolve the error (EDITOR_BUSY etc.).</summary>
        public bool Retryable { get; set; }

        /// <summary>Extension data (installedVersion, supportedVersions, ...).</summary>
        public Dictionary<string, object>? Details { get; set; }
    }

    /// <summary>Standard error codes. Provider specific codes may be added in SCREAMING_SNAKE_CASE.</summary>
    public static class AgentErrorCodes
    {
        // Input
        public const string InvalidArgument = "INVALID_ARGUMENT";
        public const string SchemaValidationFailed = "SCHEMA_VALIDATION_FAILED";

        // Resolution
        public const string ToolNotFound = "TOOL_NOT_FOUND";
        public const string ObjectNotFound = "OBJECT_NOT_FOUND";
        public const string CapabilityUnavailable = "CAPABILITY_UNAVAILABLE";

        // Permission / confirmation
        public const string PermissionDenied = "PERMISSION_DENIED";
        public const string ConfirmRequired = "CONFIRM_REQUIRED";

        // Environment
        public const string PackageVersionUnsupported = "PACKAGE_VERSION_UNSUPPORTED";
        public const string EditorBusy = "EDITOR_BUSY";
        public const string EditorNotReady = "EDITOR_NOT_READY";
        public const string EditorReloaded = "EDITOR_RELOADED";

        // Execution
        public const string ExecutionFailed = "EXECUTION_FAILED";
        public const string DryRunUnsupported = "DRY_RUN_UNSUPPORTED";
        public const string Timeout = "TIMEOUT";
        public const string Canceled = "CANCELED";

        // Infrastructure
        public const string ProtocolVersionMismatch = "PROTOCOL_VERSION_MISMATCH";
        public const string InternalError = "INTERNAL_ERROR";
    }
}

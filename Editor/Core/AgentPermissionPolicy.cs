#nullable enable

using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>Permission levels (design doc section 20).</summary>
    public enum AgentPermissionLevel
    {
        L0ReadOnly = 0,
        L1SafeEditorOperations = 1,
        L2ProjectModification = 2,
        L3Destructive = 3
    }

    /// <summary>
    /// Permission gate (design doc section 114, decision 5, default policy).
    /// v0.1 ships read-only tools, so the configurable settings surface is not
    /// built yet; this static policy implements the fixed defaults:
    /// L0/L1 allowed, L2 requires confirm (or dry run), L3 disabled.
    /// </summary>
    public static class AgentPermissionPolicy
    {
        /// <summary>Derive the level from the descriptor (decision 5). L1 requires explicit opt-in later.</summary>
        public static AgentPermissionLevel DeriveLevel(AgentToolDescriptor descriptor)
        {
            switch (descriptor.Mutation)
            {
                case AgentToolMutation.Destructive:
                    return AgentPermissionLevel.L3Destructive;
                case AgentToolMutation.Additive:
                case AgentToolMutation.Overwrite:
                    return AgentPermissionLevel.L2ProjectModification;
                default:
                    return AgentPermissionLevel.L0ReadOnly;
            }
        }

        /// <summary>
        /// Returns null when the invocation may proceed, otherwise the error to return.
        /// The dry run / confirm semantics follow section 47 (dry run wins over confirm).
        /// </summary>
        public static AgentError? Check(AgentToolDescriptor descriptor, AgentToolInvocation invocation)
        {
            var level = DeriveLevel(descriptor);

            if (level == AgentPermissionLevel.L3Destructive)
            {
                return new AgentError
                {
                    Code = AgentErrorCodes.PermissionDenied,
                    Message = $"Tool '{descriptor.Id}' is destructive (L3) and destructive tools are disabled by default.",
                    Provider = "core",
                    Retryable = false,
                };
            }

            if (level == AgentPermissionLevel.L2ProjectModification)
            {
                if (invocation.DryRun)
                {
                    if (!descriptor.SupportsDryRun)
                    {
                        return new AgentError
                        {
                            Code = AgentErrorCodes.DryRunUnsupported,
                            Message = $"Tool '{descriptor.Id}' does not support dry run.",
                            Provider = "core",
                            Retryable = false,
                        };
                    }

                    return null;
                }

                if (!invocation.Confirm)
                {
                    return new AgentError
                    {
                        Code = AgentErrorCodes.ConfirmRequired,
                        Message = $"Tool '{descriptor.Id}' modifies the project (L2); pass confirm=true (or dryRun=true).",
                        Provider = "core",
                        Retryable = false,
                    };
                }
            }

            return null;
        }
    }
}

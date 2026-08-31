#nullable enable

using System.Collections.Generic;

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>What a write tool actually changed. Attached to AgentResult for write tools.</summary>
    public sealed class MutationMetadata
    {
        /// <summary>Kind of mutation actually performed.</summary>
        public AgentToolMutation Mutation { get; set; }

        /// <summary>True when the invocation was a dry run (validate / preview only).</summary>
        public bool DryRun { get; set; }

        public List<AgentObjectRef>? Created { get; set; }

        public List<AgentObjectRef>? Modified { get; set; }

        public List<AgentObjectRef>? Deleted { get; set; }

        /// <summary>Unity undo group id when the execution was undoable.</summary>
        public int? UndoGroup { get; set; }
    }
}

namespace com.amari_noa.unity_agent_framework.sdk.contracts
{
    /// <summary>
    /// Structured object reference. Resolution priority:
    /// GlobalId -> AssetPath -> Guid + FileId -> InstanceId -> HierarchyPath.
    /// Inputs may be partial; the core always fills CanonicalUri and resolved fields on return.
    /// </summary>
    public sealed class AgentObjectRef
    {
        /// <summary>Canonical form: unity://asset/{guid}/{fileId} or unity://scene/{globalObjectId}.</summary>
        public string CanonicalUri { get; set; }

        /// <summary>GlobalObjectId string (scene objects).</summary>
        public string GlobalId { get; set; }

        public string AssetPath { get; set; }

        public string Guid { get; set; }

        /// <summary>Local file id inside the asset.</summary>
        public long? FileId { get; set; }

        /// <summary>Session-scoped hint only. Invalid across domain reloads; never use for persistence.</summary>
        public int? InstanceId { get; set; }

        /// <summary>Example: /Avatar/Armature/Hips</summary>
        public string HierarchyPath { get; set; }

        /// <summary>Fully qualified type name, e.g. UnityEngine.GameObject.</summary>
        public string Type { get; set; }

        /// <summary>Display name.</summary>
        public string Name { get; set; }
    }
}

#nullable enable

using System;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Resolves AgentObjectRef inputs and builds fully populated references
    /// (design doc sections 19 / 61; resolution priority
    /// GlobalId -> AssetPath -> Guid + FileId -> InstanceId -> HierarchyPath).
    /// Must be called on the main thread.
    /// </summary>
    public static class AgentObjectRefResolver
    {
        /// <summary>Resolve a partial reference. Returns null when nothing matches.</summary>
        public static Object? Resolve(AgentObjectRef? reference)
        {
            if (reference == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(reference.GlobalId)
                && GlobalObjectId.TryParse(reference.GlobalId, out var globalId))
            {
                var byGlobal = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                if (byGlobal != null)
                {
                    return byGlobal;
                }
            }

            if (!string.IsNullOrEmpty(reference.AssetPath))
            {
                var byPath = AssetDatabase.LoadMainAssetAtPath(reference.AssetPath);
                if (byPath != null)
                {
                    return byPath;
                }
            }

            if (!string.IsNullOrEmpty(reference.Guid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(reference.Guid);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // FileId narrowing is a future refinement; the main asset answers v0.1 reads.
                    var byGuid = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (byGuid != null)
                    {
                        return byGuid;
                    }
                }
            }

            if (reference.InstanceId.HasValue)
            {
                var byInstance = EditorUtility.InstanceIDToObject(reference.InstanceId.Value);
                if (byInstance != null)
                {
                    return byInstance;
                }
            }

            if (!string.IsNullOrEmpty(reference.HierarchyPath))
            {
                var byHierarchy = GameObject.Find(reference.HierarchyPath);
                if (byHierarchy != null)
                {
                    return byHierarchy;
                }
            }

            return null;
        }

        /// <summary>Build a fully populated reference for a live object.</summary>
        public static AgentObjectRef Describe(Object obj)
        {
            var reference = new AgentObjectRef
            {
                InstanceId = obj.GetInstanceID(),
                Type = obj.GetType().FullName,
                Name = obj.name,
            };

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                reference.AssetPath = assetPath;
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                reference.Guid = string.IsNullOrEmpty(guid) ? null : guid;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long fileId))
                {
                    reference.FileId = fileId;
                }

                if (reference.Guid != null && reference.FileId.HasValue)
                {
                    reference.CanonicalUri = $"unity://asset/{reference.Guid}/{reference.FileId.Value}";
                }
            }
            else
            {
                var globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                reference.GlobalId = globalId.ToString();
                reference.CanonicalUri = $"unity://scene/{reference.GlobalId}";
                if (obj is GameObject gameObject)
                {
                    reference.HierarchyPath = BuildHierarchyPath(gameObject.transform);
                }
                else if (obj is Component component)
                {
                    reference.HierarchyPath = BuildHierarchyPath(component.transform);
                }
            }

            return reference;
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            var path = "/" + transform.name;
            var current = transform.parent;
            while (current != null)
            {
                path = "/" + current.name + path;
                current = current.parent;
            }

            return path;
        }
    }
}

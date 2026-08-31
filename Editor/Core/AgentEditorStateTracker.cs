#nullable enable

using System;
using UnityEditor;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>Editor lifecycle states exposed to agents (design doc section 56, basic set).</summary>
    public enum AgentEditorState
    {
        Ready,
        Compiling,
        Reloading,
        PlayMode
    }

    /// <summary>
    /// Thread-safe view of the current editor state. The value is refreshed on the
    /// main thread and read from HTTP handler threads.
    /// </summary>
    public static class AgentEditorStateTracker
    {
        private static volatile AgentEditorState _current = AgentEditorState.Ready;

        /// <summary>Test hook. When set, <see cref="Current"/> returns this value.</summary>
        internal static AgentEditorState? OverrideForTests;

        public static AgentEditorState Current => OverrideForTests ?? _current;

        /// <summary>Refresh from Unity APIs. Must be called on the main thread.</summary>
        public static void RefreshFromMainThread()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _current = AgentEditorState.PlayMode;
            }
            else if (EditorApplication.isCompiling)
            {
                _current = AgentEditorState.Compiling;
            }
            else
            {
                _current = AgentEditorState.Ready;
            }
        }

        /// <summary>Mark the state explicitly (used around assembly reloads).</summary>
        public static void Set(AgentEditorState state)
        {
            _current = state;
        }
    }
}

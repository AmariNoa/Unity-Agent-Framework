#nullable enable

using UnityEditor;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>Manual recovery entry point for the local agent HTTP server (design doc watchdog).</summary>
    public static class AgentServerMenu
    {
        [MenuItem("Tools/Unity Agent Framework/Restart Agent Server")]
        private static void RestartAgentServer()
        {
            AgentCoreBootstrap.RestartServer();
        }
    }
}

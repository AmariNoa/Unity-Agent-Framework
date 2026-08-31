#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Starts the local HTTP server when the editor loads and keeps the instance
    /// descriptor up to date. The server is stopped before assembly reloads
    /// (the gateway reconnects with backoff; design doc section 22) and the
    /// descriptor is removed when the editor quits.
    /// </summary>
    [InitializeOnLoad]
    public static class AgentCoreBootstrap
    {
        private const string PortSessionKey = "UnityAgentFramework.Port";

        private static AgentHttpServer? _server;
        private static AgentToolExecutor? _executor;

        /// <summary>The live registry (rebuilt after every domain reload).</summary>
        public static AgentToolRegistry Registry { get; } = new AgentToolRegistry();

        static AgentCoreBootstrap()
        {
            AgentConsoleLogCollector.Initialize();
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnQuitting;
            EditorApplication.delayCall += StartServer;
        }

        private static void OnUpdate()
        {
            AgentEditorStateTracker.RefreshFromMainThread();
            MainThreadDispatcher.PumpOnce();
        }

        private static void StartServer()
        {
            if (_server != null)
            {
                return;
            }

            try
            {
                var token = AgentTokenStore.GetOrCreate();
                var port = ResolvePort();
                var frameworkVersion = ResolveFrameworkVersion();

                AgentToolProviderDiscovery.RegisterAll(Registry);
                _executor = new AgentToolExecutor(Registry);

                var server = new AgentHttpServer(
                    port, token, Application.unityVersion, frameworkVersion, _executor);
                server.Start();
                _server = server;
                SessionState.SetInt(PortSessionKey, port);

                var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                AgentInstanceDescriptorFile.Write(projectRoot, new AgentInstanceDescriptor
                {
                    Pid = Process.GetCurrentProcess().Id,
                    Port = port,
                    ProjectPath = projectRoot,
                    ProjectName = Path.GetFileName(projectRoot),
                    UnityVersion = Application.unityVersion,
                    Mode = "editor",
                    StartedAt = DateTimeOffset.UtcNow,
                    FrameworkVersion = frameworkVersion,
                    ProtocolVersion = AgentProtocol.Version,
                    Token = token,
                });

                UnityEngine.Debug.Log($"[UnityAgentFramework] Agent HTTP server listening on 127.0.0.1:{port}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[UnityAgentFramework] Failed to start the agent HTTP server: {e.Message}");
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            AgentEditorStateTracker.Set(AgentEditorState.Reloading);
            StopServer();
        }

        private static void OnQuitting()
        {
            StopServer();
            try
            {
                var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                AgentInstanceDescriptorFile.Delete(projectRoot);
            }
            catch (Exception)
            {
                // Best effort cleanup; the descriptor becomes stale but harmless.
            }
        }

        private static void StopServer()
        {
            if (_server == null)
            {
                return;
            }

            try
            {
                _server.Stop();
            }
            catch (Exception)
            {
                // Shutting down; a failing listener must not block the reload.
            }

            _server = null;
        }

        private static int ResolvePort()
        {
            var stored = SessionState.GetInt(PortSessionKey, 0);
            if (stored > 0 && IsPortFree(stored))
            {
                return stored;
            }

            return GetFreePort();
        }

        private static bool IsPortFree(int port)
        {
            try
            {
                var probe = new TcpListener(IPAddress.Loopback, port);
                probe.Start();
                probe.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private static string ResolveFrameworkVersion()
        {
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AgentCoreBootstrap).Assembly);
                return info?.version ?? "unknown";
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
    }
}

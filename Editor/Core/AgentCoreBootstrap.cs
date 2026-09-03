#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Starts the local HTTP server when the editor loads and keeps the instance
    /// descriptor up to date. The server is stopped before assembly reloads
    /// (the gateway reconnects with backoff; design doc section 22) and the
    /// descriptor is removed when the editor quits. Lifecycle events are also
    /// written to <see cref="AgentBootstrapLog"/> so a server that did not come
    /// back after a reload can be diagnosed later.
    /// </summary>
    [InitializeOnLoad]
    public static class AgentCoreBootstrap
    {
        private const string PortSessionKey = "UnityAgentFramework.Port";

        private static AgentHttpServer? _server;
        private static AgentToolExecutor? _executor;
        private static bool _firstUpdateLogged;
        private static bool _isQuitting;
        private static readonly AgentServerRestartPolicy RestartPolicy = new AgentServerRestartPolicy();

        /// <summary>The live registry (rebuilt after every domain reload).</summary>
        public static AgentToolRegistry Registry { get; } = new AgentToolRegistry();

        static AgentCoreBootstrap()
        {
            AgentConsoleLogCollector.Initialize();
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnQuitting;
            EditorApplication.delayCall += () => StartServer(viaWatchdog: false);
            AgentBootstrapLog.Append(ProjectRoot, $"domain loaded; StartServer scheduled via delayCall focused={IsEditorFocused}");
        }

        private static string ProjectRoot => Directory.GetParent(Application.dataPath)!.FullName;

        /// <summary>Whether the editor window currently has OS focus (diagnostic only).</summary>
        private static bool IsEditorFocused => InternalEditorUtility.isApplicationActive;

        private static void OnUpdate()
        {
            AgentEditorStateTracker.RefreshFromMainThread();
            MainThreadDispatcher.PumpOnce();

            // Recorded once per domain to tell whether the update loop runs at all
            // (e.g. while the editor is unfocused) before delayCall started the server.
            if (!_firstUpdateLogged)
            {
                _firstUpdateLogged = true;
                AgentBootstrapLog.Append(
                    ProjectRoot,
                    $"first update tick; state={AgentEditorStateTracker.Current} server={(_server != null ? "listening" : "none")} focused={IsEditorFocused}");
            }

            // Watchdog: recover from a delayCall that never ran (e.g. the editor
            // window was unfocused when the domain finished loading). Runs only
            // while the editor is idle and not shutting down, and is throttled by
            // RestartPolicy so a persistently failing start does not retry every frame.
            if (_server == null && !_isQuitting && AgentEditorStateTracker.Current == AgentEditorState.Ready)
            {
                var now = EditorApplication.timeSinceStartup;
                if (RestartPolicy.ShouldAttempt(now))
                {
                    StartServer(viaWatchdog: true);
                }
            }
        }

        private static void StartServer(bool viaWatchdog = false)
        {
            if (_server != null)
            {
                AgentBootstrapLog.Append(ProjectRoot, $"StartServer skipped; already listening on port {_server.Port}");
                return;
            }

            var trigger = viaWatchdog ? "StartServer invoked via watchdog" : "StartServer invoked";
            AgentBootstrapLog.Append(ProjectRoot, $"{trigger}; state={AgentEditorStateTracker.Current} focused={IsEditorFocused}");
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

                var projectRoot = ProjectRoot;
                var descriptor = new AgentInstanceDescriptor
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
                };
                AgentInstanceDescriptorFile.Write(projectRoot, descriptor);
                AgentMachineRegistry.Write(descriptor);
                GatewayBinaryMirror.EnsureMirrored(projectRoot);

                UnityEngine.Debug.Log($"[UnityAgentFramework] Agent HTTP server listening on 127.0.0.1:{port}");
                AgentBootstrapLog.Append(projectRoot, $"server started; port={port} frameworkVersion={frameworkVersion}");
                RestartPolicy.RecordSuccess();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[UnityAgentFramework] Failed to start the agent HTTP server: {e.Message}");
                AgentBootstrapLog.Append(ProjectRoot, $"StartServer failed: {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");

                RestartPolicy.RecordFailure(EditorApplication.timeSinceStartup);
                if (RestartPolicy.GaveUp)
                {
                    const string giveUpMessage = "watchdog gave up after 10 consecutive failures";
                    AgentBootstrapLog.Append(ProjectRoot, giveUpMessage);
                    UnityEngine.Debug.LogError($"[UnityAgentFramework] {giveUpMessage}");
                }
                else
                {
                    AgentBootstrapLog.Append(
                        ProjectRoot,
                        $"watchdog will retry in {RestartPolicy.LastDelaySeconds:0}s (failure {RestartPolicy.ConsecutiveFailures}/{AgentServerRestartPolicy.MaxConsecutiveFailures})");
                }
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            AgentEditorStateTracker.Set(AgentEditorState.Reloading);
            StopServer("beforeAssemblyReload");
        }

        /// <summary>Stops (if running) and restarts the server, clearing any watchdog give-up state.</summary>
        public static void RestartServer()
        {
            AgentBootstrapLog.Append(ProjectRoot, "manual restart requested");
            if (_server != null)
            {
                StopServer("manual restart");
            }

            RestartPolicy.Reset();
            StartServer(viaWatchdog: false);

            if (_server != null)
            {
                UnityEngine.Debug.Log($"[UnityAgentFramework] Agent HTTP server restarted on 127.0.0.1:{_server.Port}");
            }
            else
            {
                UnityEngine.Debug.LogError("[UnityAgentFramework] Manual restart failed; see bootstrap.log for details.");
            }
        }

        private static void OnQuitting()
        {
            _isQuitting = true;
            StopServer("quitting");
            try
            {
                var projectRoot = ProjectRoot;
                AgentInstanceDescriptorFile.Delete(projectRoot);
                AgentMachineRegistry.Delete(projectRoot);
            }
            catch (Exception)
            {
                // Best effort cleanup; readers drop stale entries via PID checks.
            }
        }

        private static void StopServer(string reason)
        {
            if (_server == null)
            {
                AgentBootstrapLog.Append(ProjectRoot, $"StopServer ({reason}); no server was running");
                return;
            }

            var port = _server.Port;
            try
            {
                _server.Stop();
                AgentBootstrapLog.Append(ProjectRoot, $"StopServer ({reason}); stopped port={port}");
            }
            catch (Exception e)
            {
                // Shutting down; a failing listener must not block the reload.
                AgentBootstrapLog.Append(ProjectRoot, $"StopServer ({reason}); Stop threw {e.GetType().FullName}: {e.Message}");
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

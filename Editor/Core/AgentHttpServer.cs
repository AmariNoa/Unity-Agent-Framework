#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using com.amari_noa.unity_agent_framework.sdk.serialization;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Local HTTP server for the gateway (design doc section 114, decision 2).
    /// Listens on 127.0.0.1 only, requires a Bearer token on every request
    /// (constant-time comparison, 401 otherwise) and returns 503 with a retryable
    /// error while the editor is not ready (section 56).
    /// Handlers run on listener threads; main-thread work goes through
    /// <see cref="MainThreadDispatcher"/> (tool execution arrives in Phase 2).
    /// </summary>
    public sealed class AgentHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly string _token;
        private readonly string _unityVersion;
        private readonly string _frameworkVersion;
        private readonly AgentToolExecutor _executor;
        private CancellationTokenSource? _cts;

        public int Port { get; }

        public AgentHttpServer(
            int port, string token, string unityVersion, string frameworkVersion, AgentToolExecutor executor)
        {
            Port = port;
            _token = token ?? throw new ArgumentNullException(nameof(token));
            _unityVersion = unityVersion;
            _frameworkVersion = frameworkVersion;
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener.Start();
            Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            if (_listener.IsListening)
            {
                _listener.Stop();
            }

            _listener.Close();
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (cancellation.IsCancellationRequested || !_listener.IsListening)
                {
                    return;
                }

                _ = Task.Run(() => HandleSafely(context), cancellation);
            }
        }

        private void HandleSafely(HttpListenerContext context)
        {
            try
            {
                Handle(context);
            }
            catch (Exception e)
            {
                TryWriteEnvelope(context, 200, Failure(AgentErrorCodes.InternalError, e.Message, retryable: false));
            }
        }

        private void Handle(HttpListenerContext context)
        {
            if (!IsAuthorized(context.Request))
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            var method = context.Request.HttpMethod;
            var path = context.Request.Url?.AbsolutePath ?? string.Empty;

            if (method == "GET" && path == "/api/status")
            {
                WriteEnvelope(context, 200, Success(new AgentStatusInfo
                {
                    State = AgentEditorStateTracker.Current,
                    UnityVersion = _unityVersion,
                    FrameworkVersion = _frameworkVersion,
                    ProtocolVersion = AgentProtocol.Version,
                }));
                return;
            }

            var state = AgentEditorStateTracker.Current;
            if (state != AgentEditorState.Ready)
            {
                WriteEnvelope(context, 503, Failure(
                    AgentErrorCodes.EditorNotReady,
                    $"Editor is not ready (state: {state}).",
                    retryable: true));
                return;
            }

            if (method == "GET" && path == "/api/tools")
            {
                WriteEnvelope(context, 200, Success(_executor.Registry.ListDescriptors()));
                return;
            }

            if (method == "POST" && path == "/api/invoke")
            {
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    body = reader.ReadToEnd();
                }

                var invocation = AgentToolExecutor.ParseRequest(body);
                if (invocation == null)
                {
                    WriteEnvelope(context, 200, Failure(
                        AgentErrorCodes.InvalidArgument,
                        "Request body must be a JSON object with a 'tool' id.",
                        retryable: false));
                    return;
                }

                WriteEnvelope(context, 200, _executor.Invoke(invocation));
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        private bool IsAuthorized(HttpListenerRequest request)
        {
            var header = request.Headers["Authorization"];
            const string prefix = "Bearer ";
            if (header == null || !header.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            return AgentTokenStore.ConstantTimeEquals(header.Substring(prefix.Length), _token);
        }

        private static AgentResult<T> Success<T>(T payload)
        {
            return new AgentResult<T> { Success = true, Result = payload };
        }

        private static AgentResult<object> Failure(string code, string message, bool retryable)
        {
            return new AgentResult<object>
            {
                Success = false,
                Error = new AgentError
                {
                    Code = code,
                    Message = message,
                    Provider = "core",
                    Retryable = retryable,
                },
            };
        }

        private static void WriteEnvelope<T>(HttpListenerContext context, int statusCode, AgentResult<T> envelope)
        {
            var bytes = Encoding.UTF8.GetBytes(AgentJson.Serialize(envelope));
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private static void TryWriteEnvelope<T>(HttpListenerContext context, int statusCode, AgentResult<T> envelope)
        {
            try
            {
                WriteEnvelope(context, statusCode, envelope);
            }
            catch (Exception)
            {
                // The client may already be gone; nothing meaningful can be reported here.
            }
        }
    }
}

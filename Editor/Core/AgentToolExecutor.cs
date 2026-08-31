#nullable enable

using System;
using System.Diagnostics;
using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using Newtonsoft.Json.Linq;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Runs one tool invocation: lookup, permission gate (decision 5), execution on
    /// the thread selected by the descriptor (MainThread via the serial dispatcher,
    /// section 21) and envelope finishing (ExecutionTimeMs).
    /// Called from HTTP handler threads.
    /// </summary>
    public sealed class AgentToolExecutor
    {
        /// <summary>Upper bound for one synchronous invocation (jobs arrive after v0.1).</summary>
        public static readonly TimeSpan InvocationTimeout = TimeSpan.FromSeconds(60);

        private readonly AgentToolRegistry _registry;

        public AgentToolExecutor(AgentToolRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public AgentToolRegistry Registry => _registry;

        /// <summary>
        /// Parse the canonical invoke request body:
        /// { "tool": id, "parameters": {...}, "confirm": bool, "dryRun": bool }
        /// ("dry_run" is accepted as an input alias; section 47).
        /// Returns null when the body carries no usable tool id.
        /// </summary>
        public static AgentToolInvocation? ParseRequest(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            JObject obj;
            try
            {
                obj = JObject.Parse(body);
            }
            catch (Exception)
            {
                return null;
            }

            var toolId = (string?)obj["tool"];
            if (string.IsNullOrEmpty(toolId))
            {
                return null;
            }

            var parameters = obj["parameters"];
            var parametersJson = parameters == null || parameters.Type == JTokenType.Null
                ? null
                : parameters.ToString(Newtonsoft.Json.Formatting.None);
            var confirm = obj["confirm"]?.Value<bool>() ?? false;
            var dryRun = (obj["dryRun"] ?? obj["dry_run"])?.Value<bool>() ?? false;

            return new AgentToolInvocation(toolId!, parametersJson, confirm, dryRun);
        }

        public AgentResult<object> Invoke(AgentToolInvocation invocation)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = InvokeCore(invocation);
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private AgentResult<object> InvokeCore(AgentToolInvocation invocation)
        {
            var tool = _registry.Find(invocation.ToolId);
            if (tool == null)
            {
                return Failure(AgentErrorCodes.ToolNotFound, $"Tool '{invocation.ToolId}' is not registered.");
            }

            var permissionError = AgentPermissionPolicy.Check(tool.Descriptor, invocation);
            if (permissionError != null)
            {
                return new AgentResult<object> { Success = false, Error = permissionError };
            }

            try
            {
                if (tool.Descriptor.ExecutionContext == AgentToolExecutionContext.Background)
                {
                    return tool.Handler(invocation);
                }

                var task = MainThreadDispatcher.RunAsync(() => tool.Handler(invocation));
                if (!task.Wait(InvocationTimeout))
                {
                    return Failure(
                        AgentErrorCodes.Timeout,
                        $"Tool '{invocation.ToolId}' did not finish within {InvocationTimeout.TotalSeconds:F0}s.",
                        retryable: true);
                }

                return task.Result;
            }
            catch (Exception e)
            {
                var inner = e is AggregateException aggregate ? aggregate.InnerException ?? e : e;
                return Failure(
                    AgentErrorCodes.ExecutionFailed,
                    $"Tool '{invocation.ToolId}' failed: {inner.Message}");
            }
        }

        private static AgentResult<object> Failure(string code, string message, bool retryable = false)
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
    }
}

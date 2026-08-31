using System.Threading.Tasks;
using com.amari_noa.unity_agent_framework.core.editor;
using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentToolExecutorTest
    {
        private AgentToolRegistry _registry;
        private AgentToolExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _registry = new AgentToolRegistry();
            _executor = new AgentToolExecutor(_registry);
        }

        private void RegisterEcho(
            string id,
            AgentToolExecutionContext context = AgentToolExecutionContext.Background,
            AgentToolMutation mutation = AgentToolMutation.None)
        {
            _registry.RegisterTool(new AgentToolDescriptor
            {
                Id = id,
                Provider = "core",
                ExecutionContext = context,
                Mutation = mutation,
                SupportsDryRun = true,
            },
            invocation => new AgentResult<object>
            {
                Success = true,
                Result = invocation.ParametersJson ?? "(none)",
            });
        }

        [Test]
        public void ParsesCanonicalRequestBodies()
        {
            var invocation = AgentToolExecutor.ParseRequest(
                "{\"tool\":\"unity.scene.list\",\"parameters\":{\"limit\":5},\"confirm\":true,\"dry_run\":true}");

            Assert.That(invocation.ToolId, Is.EqualTo("unity.scene.list"));
            Assert.That(invocation.ParametersJson, Is.EqualTo("{\"limit\":5}"));
            Assert.That(invocation.Confirm, Is.True);
            Assert.That(invocation.DryRun, Is.True, "dry_run must be accepted as an input alias");

            Assert.That(AgentToolExecutor.ParseRequest(""), Is.Null);
            Assert.That(AgentToolExecutor.ParseRequest("not json"), Is.Null);
            Assert.That(AgentToolExecutor.ParseRequest("{\"parameters\":{}}"), Is.Null);
        }

        [Test]
        public void InvokesBackgroundToolsAndMeasuresTime()
        {
            RegisterEcho("unity.test.get");

            var result = _executor.Invoke(new AgentToolInvocation("unity.test.get", "{\"a\":1}", false, false));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo("{\"a\":1}"));
            Assert.That(result.ExecutionTimeMs, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ReturnsToolNotFoundForUnknownTools()
        {
            var result = _executor.Invoke(new AgentToolInvocation("unity.none.get", null, false, false));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(AgentErrorCodes.ToolNotFound));
        }

        [Test]
        public void AppliesThePermissionGate()
        {
            RegisterEcho("unity.test.modify", mutation: AgentToolMutation.Overwrite);

            var denied = _executor.Invoke(new AgentToolInvocation("unity.test.modify", null, false, false));
            Assert.That(denied.Success, Is.False);
            Assert.That(denied.Error.Code, Is.EqualTo(AgentErrorCodes.ConfirmRequired));

            var allowed = _executor.Invoke(new AgentToolInvocation("unity.test.modify", null, true, false));
            Assert.That(allowed.Success, Is.True);
        }

        [Test]
        public void WrapsHandlerExceptionsAsExecutionFailed()
        {
            _registry.RegisterTool(new AgentToolDescriptor
            {
                Id = "unity.test.get",
                Provider = "core",
                ExecutionContext = AgentToolExecutionContext.Background,
                Mutation = AgentToolMutation.None,
            },
            invocation => throw new System.InvalidOperationException("boom"));

            var result = _executor.Invoke(new AgentToolInvocation("unity.test.get", null, false, false));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo(AgentErrorCodes.ExecutionFailed));
            StringAssert.Contains("boom", result.Error.Message);
        }

        [Test]
        public void RunsMainThreadToolsThroughTheDispatcher()
        {
            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int? handlerThreadId = null;
            _registry.RegisterTool(new AgentToolDescriptor
            {
                Id = "unity.test.get",
                Provider = "core",
                ExecutionContext = AgentToolExecutionContext.MainThread,
                Mutation = AgentToolMutation.None,
            },
            invocation =>
            {
                handlerThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return new AgentResult<object> { Success = true };
            });

            // Invoke from a worker thread and pump the dispatcher from this (main) thread.
            var invokeTask = Task.Run(() =>
                _executor.Invoke(new AgentToolInvocation("unity.test.get", null, false, false)));
            var deadline = System.DateTime.UtcNow.AddSeconds(10);
            while (!invokeTask.IsCompleted && System.DateTime.UtcNow < deadline)
            {
                MainThreadDispatcher.PumpOnce();
                System.Threading.Thread.Sleep(10);
            }

            Assert.That(invokeTask.IsCompleted, Is.True, "invocation did not finish in time");
            Assert.That(invokeTask.Result.Success, Is.True);
            Assert.That(handlerThreadId, Is.EqualTo(mainThreadId));
        }
    }
}

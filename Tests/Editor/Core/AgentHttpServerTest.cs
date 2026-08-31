using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentHttpServerTest
    {
        private const string Token = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        private AgentHttpServer _server;
        private HttpClient _client;
        private string _baseUrl;

        [SetUp]
        public void SetUp()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            _server = new AgentHttpServer(port, Token, "2022.3.22f1", "0.1.0-test");
            _server.Start();
            _baseUrl = $"http://127.0.0.1:{port}";
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            AgentEditorStateTracker.OverrideForTests = AgentEditorState.Ready;
        }

        [TearDown]
        public void TearDown()
        {
            AgentEditorStateTracker.OverrideForTests = null;
            _client.Dispose();
            _server.Dispose();
        }

        private HttpResponseMessage Send(HttpMethod method, string path, string bearer = Token, string body = null)
        {
            var request = new HttpRequestMessage(method, _baseUrl + path);
            if (bearer != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }

            if (body != null)
            {
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            return _client.SendAsync(request).GetAwaiter().GetResult();
        }

        private static string ReadBody(HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void RejectsMissingAndInvalidTokensWith401()
        {
            Assert.That(Send(HttpMethod.Get, "/api/status", bearer: null).StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(Send(HttpMethod.Get, "/api/status", bearer: "wrong-token").StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void ReturnsStatusEnvelopeWithEditorState()
        {
            var response = Send(HttpMethod.Get, "/api/status");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var body = ReadBody(response);
            StringAssert.Contains("\"success\":true", body);
            StringAssert.Contains("\"state\":\"ready\"", body);
            StringAssert.Contains("\"protocolVersion\":\"1.0.0\"", body);
        }

        [Test]
        public void ReturnsEmptyToolListWhileRegistryIsNotWired()
        {
            var response = Send(HttpMethod.Get, "/api/tools");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            StringAssert.Contains("\"result\":[]", ReadBody(response));
        }

        [Test]
        public void Returns503RetryableWhileEditorIsNotReady()
        {
            AgentEditorStateTracker.OverrideForTests = AgentEditorState.Compiling;

            var response = Send(HttpMethod.Get, "/api/tools");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            var body = ReadBody(response);
            StringAssert.Contains("\"code\":\"EDITOR_NOT_READY\"", body);
            StringAssert.Contains("\"retryable\":true", body);
        }

        [Test]
        public void StatusStaysAvailableWhileEditorIsNotReady()
        {
            AgentEditorStateTracker.OverrideForTests = AgentEditorState.Compiling;

            var response = Send(HttpMethod.Get, "/api/status");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            StringAssert.Contains("\"state\":\"compiling\"", ReadBody(response));
        }

        [Test]
        public void ReturnsToolNotFoundForUnknownInvoke()
        {
            var response = Send(HttpMethod.Post, "/api/invoke", body: "{\"tool\":\"unity.scene.list\"}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var body = ReadBody(response);
            StringAssert.Contains("\"success\":false", body);
            StringAssert.Contains("\"code\":\"TOOL_NOT_FOUND\"", body);
            StringAssert.Contains("unity.scene.list", body);
        }

        [Test]
        public void Returns404ForUnknownRoutes()
        {
            Assert.That(Send(HttpMethod.Get, "/api/unknown").StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}

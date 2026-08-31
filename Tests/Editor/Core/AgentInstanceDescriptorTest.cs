using System;
using System.IO;
using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentInstanceDescriptorTest
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "uaf-descriptor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Test]
        public void WritesCamelCaseDescriptorAndDeletesIt()
        {
            AgentInstanceDescriptorFile.Write(_root, new AgentInstanceDescriptor
            {
                Pid = 1234,
                Port = 45678,
                ProjectName = "TestProject",
                UnityVersion = "2022.3.22f1",
                Mode = "editor",
                StartedAt = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
                ProtocolVersion = "1.0.0",
                Token = "deadbeef",
            });

            var path = AgentInstanceDescriptorFile.GetPath(_root);
            Assert.That(File.Exists(path), Is.True);

            var json = File.ReadAllText(path);
            StringAssert.Contains("\"pid\":1234", json);
            StringAssert.Contains("\"port\":45678", json);
            StringAssert.Contains("\"token\":\"deadbeef\"", json);
            StringAssert.Contains("\"protocolVersion\":\"1.0.0\"", json);
            StringAssert.DoesNotContain("ProjectPath", json);

            AgentInstanceDescriptorFile.Delete(_root);
            Assert.That(File.Exists(path), Is.False);
        }
    }
}

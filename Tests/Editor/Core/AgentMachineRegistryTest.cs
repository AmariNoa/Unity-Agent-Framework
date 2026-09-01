using System;
using System.IO;
using com.amari_noa.unity_agent_framework.core.editor;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentMachineRegistryTest
    {
        private string _baseDir;

        [SetUp]
        public void SetUp()
        {
            _baseDir = Path.Combine(Path.GetTempPath(), "uaf-machinereg-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_baseDir))
            {
                Directory.Delete(_baseDir, recursive: true);
            }
        }

        [Test]
        public void WritesOverwritesAndDeletesEntries()
        {
            var projectRoot = Path.Combine(Path.GetTempPath(), "ProjectA");
            var descriptor = new AgentInstanceDescriptor
            {
                Pid = 100, Port = 1111, ProjectPath = projectRoot, ProjectName = "ProjectA", Token = "t1",
            };

            AgentMachineRegistry.Write(descriptor, _baseDir);
            var path = AgentMachineRegistry.GetEntryPath(projectRoot, _baseDir);
            Assert.That(File.Exists(path), Is.True);
            StringAssert.Contains("\"port\":1111", File.ReadAllText(path));

            descriptor.Port = 2222;
            AgentMachineRegistry.Write(descriptor, _baseDir);
            StringAssert.Contains("\"port\":2222", File.ReadAllText(path));
            Assert.That(Directory.GetFiles(AgentMachineRegistry.GetInstancesDirectory(_baseDir)).Length,
                Is.EqualTo(1), "overwrite must not create a second entry");

            AgentMachineRegistry.Delete(projectRoot, _baseDir);
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void EntryPathIsStableAndCaseInsensitiveOnPathText()
        {
            var a = AgentMachineRegistry.GetEntryPath(@"C:\Work\ProjectA", _baseDir);
            var b = AgentMachineRegistry.GetEntryPath(@"C:\Work\ProjectA\", _baseDir);
            var c = AgentMachineRegistry.GetEntryPath(@"c:\work\projecta", _baseDir);
            var other = AgentMachineRegistry.GetEntryPath(@"C:\Work\ProjectB", _baseDir);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.EqualTo(c));
            Assert.That(a, Is.Not.EqualTo(other));
        }

        [Test]
        public void RejectsDescriptorsWithoutProjectPath()
        {
            Assert.Throws<ArgumentException>(() =>
                AgentMachineRegistry.Write(new AgentInstanceDescriptor { Pid = 1, Port = 1 }, _baseDir));
        }
    }
}

using System;
using System.IO;
using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentBootstrapLogTest
    {
        private string _root;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "uaf-bootlog-" + Guid.NewGuid().ToString("N"));
            _path = AgentBootstrapLog.GetPath(_root);
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
        public void ResolvesPathUnderLibraryUnityAgent()
        {
            Assert.That(_path, Is.EqualTo(Path.Combine(_root, "Library", "UnityAgent", "bootstrap.log")));
        }

        [Test]
        public void FormatsTimestampPidAndIndentsContinuationLines()
        {
            var timestamp = new DateTimeOffset(2026, 9, 2, 10, 10, 5, 123, TimeSpan.FromHours(9));

            var entry = AgentBootstrapLog.FormatEntry(timestamp, 42, "StartServer failed: X\r\n  at A\n  at B");

            Assert.That(entry, Is.EqualTo(
                "2026-09-02T10:10:05.123+09:00 pid=42 StartServer failed: X\n" +
                "      at A\n" +
                "      at B\n"));
        }

        [Test]
        public void AppendsEntriesAndCreatesDirectory()
        {
            AgentBootstrapLog.AppendCore(_path, "first", AgentBootstrapLog.DefaultMaxBytes);
            AgentBootstrapLog.AppendCore(_path, "second", AgentBootstrapLog.DefaultMaxBytes);

            var lines = File.ReadAllLines(_path);
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Does.Match(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2} pid=\d+ first$"));
            Assert.That(lines[1], Does.EndWith(" second"));
        }

        [Test]
        public void RotatesOneGenerationWhenExceedingMaxBytes()
        {
            AgentBootstrapLog.AppendCore(_path, "old-1", maxBytes: 200);
            AgentBootstrapLog.AppendCore(_path, new string('x', 300), maxBytes: 200);
            AgentBootstrapLog.AppendCore(_path, "new-1", maxBytes: 200);
            AgentBootstrapLog.AppendCore(_path, new string('y', 300), maxBytes: 200);
            AgentBootstrapLog.AppendCore(_path, "new-2", maxBytes: 200);

            var rotated = _path + ".1";
            Assert.That(File.Exists(rotated), Is.True);
            Assert.That(File.Exists(_path + ".2"), Is.False);
            Assert.That(File.ReadAllText(rotated), Does.Contain("yyy").And.Not.Contain("old-1"));
            Assert.That(File.ReadAllLines(_path), Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(_path), Does.EndWith(" new-2\n"));
        }
    }
}

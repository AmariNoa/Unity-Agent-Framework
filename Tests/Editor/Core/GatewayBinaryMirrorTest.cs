using System;
using System.IO;
using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class GatewayBinaryMirrorTest
    {
        private string _baseDir;
        private string _sourceDir;

        [SetUp]
        public void SetUp()
        {
            _baseDir = Path.Combine(Path.GetTempPath(), "uaf-mirror-" + Guid.NewGuid().ToString("N"));
            _sourceDir = Path.Combine(_baseDir, "source");
            Directory.CreateDirectory(_sourceDir);
            File.WriteAllText(Path.Combine(_sourceDir, "unity-agent-gateway.exe"), "binary-v1");
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
        public void ComparesSemverIncludingPrereleases()
        {
            Assert.That(GatewayBinaryMirror.CompareSemver("0.1.0", "0.1.0"), Is.EqualTo(0));
            Assert.That(GatewayBinaryMirror.CompareSemver("0.2.0", "0.1.9"), Is.GreaterThan(0));
            Assert.That(GatewayBinaryMirror.CompareSemver("0.1.0", "0.1.0-alpha.1"), Is.GreaterThan(0));
            Assert.That(GatewayBinaryMirror.CompareSemver("0.1.0-alpha.1", "0.1.0-alpha.2"), Is.LessThan(0));
            Assert.That(GatewayBinaryMirror.CompareSemver("1.0.0+build5", "1.0.0"), Is.EqualTo(0));
        }

        [Test]
        public void MirrorsAndPromotesToCurrent()
        {
            GatewayBinaryMirror.Mirror(_sourceDir, "0.1.0-alpha.1", _baseDir);

            var root = GatewayBinaryMirror.GetGatewayRootDirectory(_baseDir);
            Assert.That(File.Exists(Path.Combine(root, "0.1.0-alpha.1", "unity-agent-gateway.exe")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "current", "unity-agent-gateway.exe")), Is.True);
            Assert.That(GatewayBinaryMirror.ReadCurrentVersion(_baseDir), Is.EqualTo("0.1.0-alpha.1"));
        }

        [Test]
        public void NeverDowngradesCurrent()
        {
            GatewayBinaryMirror.Mirror(_sourceDir, "0.2.0", _baseDir);

            File.WriteAllText(Path.Combine(_sourceDir, "unity-agent-gateway.exe"), "binary-old");
            GatewayBinaryMirror.Mirror(_sourceDir, "0.1.0", _baseDir);

            var root = GatewayBinaryMirror.GetGatewayRootDirectory(_baseDir);
            Assert.That(GatewayBinaryMirror.ReadCurrentVersion(_baseDir), Is.EqualTo("0.2.0"));
            Assert.That(File.ReadAllText(Path.Combine(root, "current", "unity-agent-gateway.exe")),
                Is.EqualTo("binary-v1"));
            Assert.That(Directory.Exists(Path.Combine(root, "0.1.0")), Is.True,
                "older versions stay mirrored side by side");
        }

        [Test]
        public void UpgradesCurrentWhenNewerVersionArrives()
        {
            GatewayBinaryMirror.Mirror(_sourceDir, "0.1.0", _baseDir);

            File.WriteAllText(Path.Combine(_sourceDir, "unity-agent-gateway.exe"), "binary-v2");
            GatewayBinaryMirror.Mirror(_sourceDir, "0.2.0", _baseDir);

            var root = GatewayBinaryMirror.GetGatewayRootDirectory(_baseDir);
            Assert.That(GatewayBinaryMirror.ReadCurrentVersion(_baseDir), Is.EqualTo("0.2.0"));
            Assert.That(File.ReadAllText(Path.Combine(root, "current", "unity-agent-gateway.exe")),
                Is.EqualTo("binary-v2"));
        }
    }
}

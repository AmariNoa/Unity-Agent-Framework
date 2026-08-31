using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class GatewayInstallationCheckTest
    {
        [Test]
        public void ExpectedRidReturnsAKnownRid()
        {
            var rid = GatewayInstallationCheck.ExpectedRid();
            Assert.That(new[] { "win-x64", "osx-arm64", "osx-x64", "linux-x64" }, Does.Contain(rid));
        }

        [Test]
        public void EvaluatesInstallationStates()
        {
            const string prefix = GatewayInstallationCheck.GatewayPackagePrefix;

            Assert.That(GatewayInstallationCheck.Evaluate(new string[0], "win-x64", null),
                Is.EqualTo(GatewayInstallationStatus.NotInstalled));

            Assert.That(GatewayInstallationCheck.Evaluate(new[] { prefix + "osx-arm64" }, "win-x64", null),
                Is.EqualTo(GatewayInstallationStatus.PlatformMismatch));

            Assert.That(GatewayInstallationCheck.Evaluate(new[] { prefix + "win-x64" }, "win-x64", null),
                Is.EqualTo(GatewayInstallationStatus.Installed));
        }

        [Test]
        public void EvaluateDetectsMissingBinaryUnderAProjectRoot()
        {
            const string prefix = GatewayInstallationCheck.GatewayPackagePrefix;
            var root = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "uaf-gwcheck-" + System.Guid.NewGuid().ToString("N"));
            var packageDir = System.IO.Path.Combine(root, "Packages", prefix + "win-x64");
            System.IO.Directory.CreateDirectory(packageDir);
            try
            {
                Assert.That(GatewayInstallationCheck.Evaluate(new[] { prefix + "win-x64" }, "win-x64", root),
                    Is.EqualTo(GatewayInstallationStatus.BinaryMissing));

                var gatewayDir = System.IO.Path.Combine(packageDir, "Gateway~");
                System.IO.Directory.CreateDirectory(gatewayDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(gatewayDir, "unity-agent-gateway.exe"), "x");

                Assert.That(GatewayInstallationCheck.Evaluate(new[] { prefix + "win-x64" }, "win-x64", root),
                    Is.EqualTo(GatewayInstallationStatus.Installed));
            }
            finally
            {
                System.IO.Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void BuildsActionableWarnings()
        {
            var warning = GatewayInstallationCheck.BuildWarning(GatewayInstallationStatus.NotInstalled, "win-x64");
            Assert.That(warning, Does.Contain("com.amari-noa.unity-agent-framework.gateway.win-x64"));
            Assert.That(warning, Does.Contain("README"));
        }
    }
}

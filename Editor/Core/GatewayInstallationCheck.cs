#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>Result of the gateway package installation check.</summary>
    public enum GatewayInstallationStatus
    {
        Installed,
        NotInstalled,
        PlatformMismatch,
        BinaryMissing
    }

    /// <summary>
    /// Detects whether the platform-matching gateway package is installed
    /// (design doc section 114, decision 2: no hard dependency; missing or
    /// mismatched installations are reported via console warnings in v0.1,
    /// the guidance window arrives in v0.2).
    /// </summary>
    [InitializeOnLoad]
    public static class GatewayInstallationCheck
    {
        public const string GatewayPackagePrefix = "com.amari-noa.unity-agent-framework.gateway.";
        private const string WarnedSessionKey = "UnityAgentFramework.GatewayWarned";

        static GatewayInstallationCheck()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool(WarnedSessionKey, false))
            {
                return;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var expectedRid = ExpectedRid();
            var installed = FindInstalledGatewayPackages(projectRoot);
            var status = Evaluate(installed, expectedRid, projectRoot);
            if (status == GatewayInstallationStatus.Installed)
            {
                return;
            }

            SessionState.SetBool(WarnedSessionKey, true);
            Debug.LogWarning(BuildWarning(status, expectedRid));
        }

        /// <summary>.NET RID for the current editor platform.</summary>
        public static string ExpectedRid()
        {
            var arm = RuntimeInformation.OSArchitecture == Architecture.Arm64;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // win-arm64 is out of scope; the x64 binary runs under emulation.
                return "win-x64";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return arm ? "osx-arm64" : "osx-x64";
            }

            return "linux-x64";
        }

        /// <summary>Gateway package ids found in Packages/ (VPM installs are embedded).</summary>
        public static List<string> FindInstalledGatewayPackages(string projectRoot)
        {
            var result = new List<string>();
            var packagesDir = Path.Combine(projectRoot, "Packages");
            if (Directory.Exists(packagesDir))
            {
                result.AddRange(
                    Directory.GetDirectories(packagesDir)
                        .Select(Path.GetFileName)
                        .Where(name => name != null && name.StartsWith(GatewayPackagePrefix, StringComparison.Ordinal))
                        .Select(name => name!));
            }

            try
            {
                result.AddRange(
                    UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                        .Select(p => p.name)
                        .Where(name => name.StartsWith(GatewayPackagePrefix, StringComparison.Ordinal)));
            }
            catch (Exception)
            {
                // Package manager not ready yet; the Packages/ scan already covers VPM installs.
            }

            return result.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        /// <summary>Pure evaluation used by tests. projectRoot may be null to skip the binary check.</summary>
        public static GatewayInstallationStatus Evaluate(
            IReadOnlyCollection<string> installedGatewayPackageIds, string expectedRid, string? projectRoot)
        {
            if (installedGatewayPackageIds.Count == 0)
            {
                return GatewayInstallationStatus.NotInstalled;
            }

            var matching = GatewayPackagePrefix + expectedRid;
            if (!installedGatewayPackageIds.Contains(matching))
            {
                return GatewayInstallationStatus.PlatformMismatch;
            }

            if (projectRoot != null)
            {
                var binaryName = expectedRid.StartsWith("win", StringComparison.Ordinal)
                    ? "unity-agent-gateway.exe"
                    : "unity-agent-gateway";
                var binaryPath = Path.Combine(projectRoot, "Packages", matching, "Gateway~", binaryName);
                if (!File.Exists(binaryPath))
                {
                    return GatewayInstallationStatus.BinaryMissing;
                }
            }

            return GatewayInstallationStatus.Installed;
        }

        public static string BuildWarning(GatewayInstallationStatus status, string expectedRid)
        {
            var expectedPackage = GatewayPackagePrefix + expectedRid;
            switch (status)
            {
                case GatewayInstallationStatus.PlatformMismatch:
                    return "[UnityAgentFramework] An agent gateway package is installed but does not match " +
                           $"this platform. Install '{expectedPackage}' to let MCP clients connect. " +
                           "See the package README for setup instructions.";
                case GatewayInstallationStatus.BinaryMissing:
                    return $"[UnityAgentFramework] '{expectedPackage}' is installed but its Gateway~ binary " +
                           "is missing. Reinstall the package.";
                default:
                    return "[UnityAgentFramework] The agent gateway package is not installed. MCP clients " +
                           $"cannot connect until '{expectedPackage}' is added " +
                           "(or the gateway is run from source during development). " +
                           "See the package README for setup instructions.";
            }
        }
    }
}

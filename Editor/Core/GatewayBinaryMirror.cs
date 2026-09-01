#nullable enable

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Mirrors the installed gateway package binaries to a machine-shared,
    /// project-independent location (design doc section 114, decision 2):
    /// %LOCALAPPDATA%/UnityAgentFramework/gateway/&lt;version&gt;/ plus a
    /// "current" copy pointing at the newest mirrored version. MCP clients
    /// register the current path so the registration survives project moves.
    /// The mirror never downgrades "current"; version mismatches are caught by
    /// the connection handshake.
    /// </summary>
    public static class GatewayBinaryMirror
    {
        public const string GatewayDirectoryName = "gateway";
        public const string CurrentDirectoryName = "current";
        public const string VersionMarkerFileName = "version.txt";

        public static string GetGatewayRootDirectory(string? baseDirectory = null)
        {
            return Path.Combine(
                baseDirectory ?? AgentMachineRegistry.GetDefaultBaseDirectory(), GatewayDirectoryName);
        }

        /// <summary>
        /// Compare two semver strings. Returns &lt;0 / 0 / &gt;0. A release version
        /// ranks above a prerelease of the same triplet; prerelease identifiers
        /// compare ordinally (sufficient for this package family).
        /// </summary>
        public static int CompareSemver(string a, string b)
        {
            var (aCore, aPre) = Split(a);
            var (bCore, bPre) = Split(b);
            for (var i = 0; i < 3; i++)
            {
                var diff = aCore[i].CompareTo(bCore[i]);
                if (diff != 0)
                {
                    return diff;
                }
            }

            if (aPre == null && bPre == null)
            {
                return 0;
            }

            if (aPre == null)
            {
                return 1;
            }

            if (bPre == null)
            {
                return -1;
            }

            return string.CompareOrdinal(aPre, bPre);

            static (int[] core, string? prerelease) Split(string version)
            {
                var main = version;
                string? prerelease = null;
                var plus = main.IndexOf('+');
                if (plus >= 0)
                {
                    main = main.Substring(0, plus);
                }

                var dash = main.IndexOf('-');
                if (dash >= 0)
                {
                    prerelease = main.Substring(dash + 1);
                    main = main.Substring(0, dash);
                }

                var parts = main.Split('.');
                var core = new int[3];
                for (var i = 0; i < 3; i++)
                {
                    core[i] = i < parts.Length && int.TryParse(parts[i], out var value) ? value : 0;
                }

                return (core, prerelease);
            }
        }

        /// <summary>Version recorded for the current mirror, or null when absent.</summary>
        public static string? ReadCurrentVersion(string? baseDirectory = null)
        {
            var marker = Path.Combine(
                GetGatewayRootDirectory(baseDirectory), CurrentDirectoryName, VersionMarkerFileName);
            try
            {
                return File.Exists(marker) ? File.ReadAllText(marker).Trim() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Mirror the given package Gateway~ directory as the given version and
        /// promote it to current when it is newer than the existing current.
        /// </summary>
        public static void Mirror(string gatewaySourceDirectory, string version, string? baseDirectory = null)
        {
            var root = GetGatewayRootDirectory(baseDirectory);
            var versionDir = Path.Combine(root, version);
            if (!Directory.Exists(versionDir))
            {
                CopyDirectory(gatewaySourceDirectory, versionDir + ".tmp");
                File.WriteAllText(Path.Combine(versionDir + ".tmp", VersionMarkerFileName), version);
                Directory.Move(versionDir + ".tmp", versionDir);
            }

            var currentVersion = ReadCurrentVersion(baseDirectory);
            if (currentVersion != null && CompareSemver(version, currentVersion) <= 0)
            {
                return;
            }

            var currentDir = Path.Combine(root, CurrentDirectoryName);
            var staging = currentDir + ".new";
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }

            CopyDirectory(versionDir, staging);
            if (Directory.Exists(currentDir))
            {
                // May fail while a gateway from this copy is running; retried on the next load.
                Directory.Delete(currentDir, recursive: true);
            }

            Directory.Move(staging, currentDir);
        }

        /// <summary>
        /// Find the platform-matching gateway package in the project and mirror it.
        /// Failures are reported as warnings and never break editor startup.
        /// </summary>
        public static void EnsureMirrored(string projectRoot, string? baseDirectory = null)
        {
            try
            {
                var rid = GatewayInstallationCheck.ExpectedRid();
                var packageDir = Path.Combine(
                    projectRoot, "Packages", GatewayInstallationCheck.GatewayPackagePrefix + rid);
                var gatewayDir = Path.Combine(packageDir, "Gateway~");
                var manifest = Path.Combine(packageDir, "package.json");
                if (!Directory.Exists(gatewayDir) || !File.Exists(manifest))
                {
                    return; // Not installed; the installation check reports guidance separately.
                }

                var version = (string?)JObject.Parse(File.ReadAllText(manifest))["version"];
                if (string.IsNullOrEmpty(version))
                {
                    return;
                }

                Mirror(gatewayDir, version!, baseDirectory);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityAgentFramework] Gateway mirror update failed (retried on next load): {e.Message}");
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }
}

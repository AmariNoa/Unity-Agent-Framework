#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using com.amari_noa.unity_agent_framework.sdk.serialization;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Machine-level instance registry (design doc section 114, decision 2,
    /// multi-instance support). Every running editor mirrors its instance
    /// descriptor to %LOCALAPPDATA%/UnityAgentFramework/instances/&lt;hash&gt;.json
    /// so a single gateway registration can discover all running projects.
    /// Entries are removed on editor quit; readers additionally drop stale
    /// entries via PID liveness and status pings.
    /// </summary>
    public static class AgentMachineRegistry
    {
        public const string InstancesDirectoryName = "instances";

        public static string GetDefaultBaseDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UnityAgentFramework");
        }

        public static string GetInstancesDirectory(string? baseDirectory = null)
        {
            return Path.Combine(baseDirectory ?? GetDefaultBaseDirectory(), InstancesDirectoryName);
        }

        /// <summary>Stable entry file name derived from the project root path.</summary>
        public static string GetEntryPath(string projectRoot, string? baseDirectory = null)
        {
            var normalized = Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized.ToLowerInvariant()));
            var builder = new StringBuilder(32);
            for (var i = 0; i < 16; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return Path.Combine(GetInstancesDirectory(baseDirectory), builder + ".json");
        }

        public static void Write(AgentInstanceDescriptor descriptor, string? baseDirectory = null)
        {
            if (string.IsNullOrEmpty(descriptor.ProjectPath))
            {
                throw new ArgumentException("Descriptor must carry the project path.", nameof(descriptor));
            }

            var path = GetEntryPath(descriptor.ProjectPath!, baseDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var temp = path + ".tmp";
            File.WriteAllText(temp, AgentJson.Serialize(descriptor));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temp, path);
        }

        public static void Delete(string projectRoot, string? baseDirectory = null)
        {
            var path = GetEntryPath(projectRoot, baseDirectory);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}

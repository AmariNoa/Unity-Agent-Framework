#nullable enable

using System;
using System.IO;
using com.amari_noa.unity_agent_framework.sdk.serialization;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Instance descriptor payload (design doc section 51; superset of the official
    /// pipeline fields). Written under Library/UnityAgent/ so it is never committed.
    /// </summary>
    public sealed class AgentInstanceDescriptor
    {
        public int Pid { get; set; }
        public int Port { get; set; }
        public string? ProjectPath { get; set; }
        public string? ProjectName { get; set; }
        public string? UnityVersion { get; set; }
        public string? Mode { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public string? FrameworkVersion { get; set; }
        public string? ProtocolVersion { get; set; }
        public string? Token { get; set; }
    }

    /// <summary>Writes and removes the instance descriptor file.</summary>
    public static class AgentInstanceDescriptorFile
    {
        public const string RelativePath = "Library/UnityAgent/instance.json";

        public static string GetPath(string projectRoot)
        {
            return Path.Combine(projectRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        public static void Write(string projectRoot, AgentInstanceDescriptor descriptor)
        {
            var path = GetPath(projectRoot);
            var directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temp file first so readers never observe a partial file.
            var temp = path + ".tmp";
            File.WriteAllText(temp, AgentJson.Serialize(descriptor));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temp, path);
        }

        public static void Delete(string projectRoot)
        {
            var path = GetPath(projectRoot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}

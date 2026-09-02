#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Append-only diagnostic log for the agent server lifecycle
    /// (Library/UnityAgent/bootstrap.log). Console entries are lost on domain
    /// reload and Editor.log is shared between editor instances, so start /
    /// stop events are also written here to diagnose a server that did not
    /// come back after a reload. One rotation generation (bootstrap.log.1)
    /// keeps the file bounded. Logging must never break the server startup:
    /// failures are reported once as a console warning.
    /// </summary>
    public static class AgentBootstrapLog
    {
        public const string RelativePath = "Library/UnityAgent/bootstrap.log";
        public const long DefaultMaxBytes = 256 * 1024;

        private static bool _warned;

        public static string GetPath(string projectRoot)
        {
            return Path.Combine(projectRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>Append one entry; swallows I/O errors after warning once.</summary>
        public static void Append(string projectRoot, string message)
        {
            try
            {
                AppendCore(GetPath(projectRoot), message, DefaultMaxBytes);
            }
            catch (Exception e)
            {
                if (_warned)
                {
                    return;
                }

                _warned = true;
                UnityEngine.Debug.LogWarning(
                    $"[UnityAgentFramework] Failed to write the bootstrap log ({RelativePath}): {e.Message}");
            }
        }

        /// <summary>
        /// Append one timestamped entry (continuation lines of a multi-line
        /// message are indented). Rotates the file to "&lt;path&gt;.1" when it
        /// exceeds <paramref name="maxBytes"/> before appending. Throws on I/O errors.
        /// </summary>
        public static void AppendCore(string path, string message, long maxBytes)
        {
            var directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            RotateIfNeeded(path, maxBytes);
            File.AppendAllText(path, FormatEntry(DateTimeOffset.Now, Process.GetCurrentProcess().Id, message));
        }

        public static string FormatEntry(DateTimeOffset timestamp, int pid, string message)
        {
            var lines = (message ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            var builder = new StringBuilder();
            builder.Append(timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"))
                .Append(" pid=").Append(pid)
                .Append(' ').Append(lines[0]).Append('\n');
            for (var i = 1; i < lines.Length; i++)
            {
                builder.Append("    ").Append(lines[i]).Append('\n');
            }

            return builder.ToString();
        }

        private static void RotateIfNeeded(string path, long maxBytes)
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= maxBytes)
            {
                return;
            }

            var rotated = path + ".1";
            if (File.Exists(rotated))
            {
                File.Delete(rotated);
            }

            File.Move(path, rotated);
        }
    }
}

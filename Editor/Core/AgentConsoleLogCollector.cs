#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>One collected console entry.</summary>
    public sealed class AgentConsoleEntry
    {
        public DateTimeOffset Time { get; set; }
        public string? Type { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Ring buffer of console messages for unity.console.get. Unity exposes no
    /// public console-read API, so entries are collected from
    /// Application.logMessageReceivedThreaded starting at editor load
    /// (earlier messages are not observable; documented limitation).
    /// </summary>
    public static class AgentConsoleLogCollector
    {
        public const int Capacity = 1000;

        private static readonly object Gate = new object();
        private static readonly Queue<AgentConsoleEntry> Entries = new Queue<AgentConsoleEntry>(Capacity);
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            var entry = new AgentConsoleEntry
            {
                Time = DateTimeOffset.UtcNow,
                Type = ToTypeName(type),
                Message = condition,
            };

            lock (Gate)
            {
                if (Entries.Count >= Capacity)
                {
                    Entries.Dequeue();
                }

                Entries.Enqueue(entry);
            }
        }

        private static string ToTypeName(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    return "error";
                case LogType.Assert:
                    return "assert";
                case LogType.Warning:
                    return "warning";
                default:
                    return "log";
            }
        }

        /// <summary>Snapshot of the collected entries, oldest first.</summary>
        public static List<AgentConsoleEntry> Snapshot()
        {
            lock (Gate)
            {
                return new List<AgentConsoleEntry>(Entries);
            }
        }

        internal static void ClearForTests()
        {
            lock (Gate)
            {
                Entries.Clear();
            }
        }
    }
}

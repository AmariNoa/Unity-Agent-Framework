#nullable enable

using System;
using System.Security.Cryptography;
using UnityEditor;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Bearer token for the local HTTP server. Generated once per editor session
    /// (SessionState survives domain reloads and is cleared when the editor exits;
    /// design doc section 114, decision 5).
    /// </summary>
    public static class AgentTokenStore
    {
        private const string SessionKey = "UnityAgentFramework.BearerToken";

        public static string GetOrCreate()
        {
            var existing = SessionState.GetString(SessionKey, string.Empty);
            if (!string.IsNullOrEmpty(existing))
            {
                return existing;
            }

            var token = GenerateToken();
            SessionState.SetString(SessionKey, token);
            return token;
        }

        internal static void ClearForTests()
        {
            SessionState.EraseString(SessionKey);
        }

        private static string GenerateToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var chars = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = ToHex(bytes[i] >> 4);
                chars[i * 2 + 1] = ToHex(bytes[i] & 0xF);
            }

            return new string(chars);
        }

        private static char ToHex(int value)
        {
            return (char)(value < 10 ? '0' + value : 'a' + value - 10);
        }

        /// <summary>Constant-time comparison (design doc section 52).</summary>
        public static bool ConstantTimeEquals(string? a, string? b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}

#nullable enable

using System;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Pure backoff/retry decision logic for the <see cref="AgentCoreBootstrap"/>
    /// watchdog. "Now" is passed in (EditorApplication.timeSinceStartup) rather
    /// than read internally so the policy can be unit-tested without the editor
    /// update loop. An instance lives in a static field of the bootstrap, so its
    /// state resets naturally on a domain reload.
    /// </summary>
    public sealed class AgentServerRestartPolicy
    {
        public const double InitialDelaySeconds = 2;
        public const double MaxDelaySeconds = 60;
        public const int MaxConsecutiveFailures = 10;

        private double _currentDelaySeconds = InitialDelaySeconds;
        private double _nextAttemptAt;

        /// <summary>Number of failures since the last success (or since the policy was reset).</summary>
        public int ConsecutiveFailures { get; private set; }

        /// <summary>True once <see cref="MaxConsecutiveFailures"/> has been reached; automatic retries stop.</summary>
        public bool GaveUp { get; private set; }

        /// <summary>The delay scheduled after the most recent failure (seconds); for logging.</summary>
        public double LastDelaySeconds => _currentDelaySeconds;

        /// <summary>Whether a start attempt is allowed at <paramref name="now"/>.</summary>
        public bool ShouldAttempt(double now)
        {
            return !GaveUp && now >= _nextAttemptAt;
        }

        /// <summary>Record a failed start attempt and schedule (or give up on) the next one.</summary>
        public void RecordFailure(double now)
        {
            ConsecutiveFailures++;
            if (ConsecutiveFailures >= MaxConsecutiveFailures)
            {
                GaveUp = true;
                return;
            }

            _currentDelaySeconds = ConsecutiveFailures <= 1
                ? InitialDelaySeconds
                : Math.Min(_currentDelaySeconds * 2, MaxDelaySeconds);
            _nextAttemptAt = now + _currentDelaySeconds;
        }

        /// <summary>Record a successful start; clears the failure count and give-up state.</summary>
        public void RecordSuccess()
        {
            Reset();
        }

        /// <summary>Clear give-up and counters (used by the manual restart menu and on success).</summary>
        public void Reset()
        {
            ConsecutiveFailures = 0;
            GaveUp = false;
            _currentDelaySeconds = InitialDelaySeconds;
            _nextAttemptAt = 0;
        }
    }
}

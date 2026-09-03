using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentServerRestartPolicyTest
    {
        [Test]
        public void AllowsTheFirstAttemptImmediately()
        {
            var policy = new AgentServerRestartPolicy();

            Assert.That(policy.ShouldAttempt(0), Is.True);
            Assert.That(policy.ShouldAttempt(1000), Is.True);
        }

        [Test]
        public void AfterOneFailureWaitsTheInitialDelay()
        {
            var policy = new AgentServerRestartPolicy();

            policy.RecordFailure(now: 100);

            Assert.That(policy.ShouldAttempt(100 + AgentServerRestartPolicy.InitialDelaySeconds - 0.001), Is.False);
            Assert.That(policy.ShouldAttempt(100 + AgentServerRestartPolicy.InitialDelaySeconds), Is.True);
        }

        [Test]
        public void DelaysDoubleUpToTheCapThenHoldAtTheCap()
        {
            var policy = new AgentServerRestartPolicy();
            double now = 0;
            double[] expectedDelays = { 2, 4, 8, 16, 32, 60, 60 };

            foreach (var expectedDelay in expectedDelays)
            {
                policy.RecordFailure(now);
                Assert.That(policy.LastDelaySeconds, Is.EqualTo(expectedDelay));
                now += expectedDelay;
            }
        }

        [Test]
        public void SuccessResetsTheFailureCounter()
        {
            var policy = new AgentServerRestartPolicy();
            policy.RecordFailure(now: 0);
            policy.RecordFailure(now: 2);
            Assert.That(policy.ConsecutiveFailures, Is.EqualTo(2));

            policy.RecordSuccess();

            Assert.That(policy.ConsecutiveFailures, Is.EqualTo(0));
            Assert.That(policy.GaveUp, Is.False);
            Assert.That(policy.ShouldAttempt(0), Is.True);
        }

        [Test]
        public void GivesUpAfterTenConsecutiveFailures()
        {
            var policy = new AgentServerRestartPolicy();
            double now = 0;

            for (var i = 0; i < AgentServerRestartPolicy.MaxConsecutiveFailures; i++)
            {
                Assert.That(policy.GaveUp, Is.False, $"should not have given up before failure {i + 1}");
                policy.RecordFailure(now);
                now += 1;
            }

            Assert.That(policy.GaveUp, Is.True);
            Assert.That(policy.ConsecutiveFailures, Is.EqualTo(AgentServerRestartPolicy.MaxConsecutiveFailures));
            Assert.That(policy.ShouldAttempt(now + 1_000_000), Is.False);
        }

        [Test]
        public void ResetClearsGiveUpState()
        {
            var policy = new AgentServerRestartPolicy();
            for (var i = 0; i < AgentServerRestartPolicy.MaxConsecutiveFailures; i++)
            {
                policy.RecordFailure(now: i);
            }

            Assert.That(policy.GaveUp, Is.True);

            policy.Reset();

            Assert.That(policy.GaveUp, Is.False);
            Assert.That(policy.ConsecutiveFailures, Is.EqualTo(0));
            Assert.That(policy.ShouldAttempt(0), Is.True);
        }
    }
}

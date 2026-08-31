using com.amari_noa.unity_agent_framework.core.editor;
using com.amari_noa.unity_agent_framework.sdk;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentPermissionPolicyTest
    {
        private static AgentToolDescriptor Descriptor(AgentToolMutation mutation, bool supportsDryRun = true)
        {
            return new AgentToolDescriptor
            {
                Id = "unity.test.modify",
                Mutation = mutation,
                SupportsDryRun = supportsDryRun,
            };
        }

        private static AgentToolInvocation Invocation(bool confirm = false, bool dryRun = false)
        {
            return new AgentToolInvocation("unity.test.modify", null, confirm, dryRun);
        }

        [Test]
        public void DerivesLevelsFromMutation()
        {
            Assert.That(AgentPermissionPolicy.DeriveLevel(Descriptor(AgentToolMutation.None)),
                Is.EqualTo(AgentPermissionLevel.L0ReadOnly));
            Assert.That(AgentPermissionPolicy.DeriveLevel(Descriptor(AgentToolMutation.Additive)),
                Is.EqualTo(AgentPermissionLevel.L2ProjectModification));
            Assert.That(AgentPermissionPolicy.DeriveLevel(Descriptor(AgentToolMutation.Overwrite)),
                Is.EqualTo(AgentPermissionLevel.L2ProjectModification));
            Assert.That(AgentPermissionPolicy.DeriveLevel(Descriptor(AgentToolMutation.Destructive)),
                Is.EqualTo(AgentPermissionLevel.L3Destructive));
        }

        [Test]
        public void AllowsReadOnlyToolsWithoutConfirm()
        {
            Assert.That(AgentPermissionPolicy.Check(Descriptor(AgentToolMutation.None), Invocation()), Is.Null);
        }

        [Test]
        public void RequiresConfirmForProjectModification()
        {
            var error = AgentPermissionPolicy.Check(Descriptor(AgentToolMutation.Additive), Invocation());
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Code, Is.EqualTo(AgentErrorCodes.ConfirmRequired));

            Assert.That(AgentPermissionPolicy.Check(
                Descriptor(AgentToolMutation.Additive), Invocation(confirm: true)), Is.Null);
        }

        [Test]
        public void DryRunWinsOverConfirmForProjectModification()
        {
            Assert.That(AgentPermissionPolicy.Check(
                Descriptor(AgentToolMutation.Additive), Invocation(dryRun: true)), Is.Null);

            var error = AgentPermissionPolicy.Check(
                Descriptor(AgentToolMutation.Additive, supportsDryRun: false), Invocation(dryRun: true));
            Assert.That(error.Code, Is.EqualTo(AgentErrorCodes.DryRunUnsupported));
        }

        [Test]
        public void DeniesDestructiveToolsByDefault()
        {
            var error = AgentPermissionPolicy.Check(
                Descriptor(AgentToolMutation.Destructive), Invocation(confirm: true));
            Assert.That(error.Code, Is.EqualTo(AgentErrorCodes.PermissionDenied));
        }
    }
}

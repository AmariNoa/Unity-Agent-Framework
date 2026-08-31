using System.Collections.Generic;
using com.amari_noa.unity_agent_framework.core.editor;
using com.amari_noa.unity_agent_framework.sdk.contracts;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentPaginationTest
    {
        private static readonly List<int> Source = new List<int> { 1, 2, 3, 4, 5 };

        [Test]
        public void AppliesDefaultsAndSlices()
        {
            var items = AgentPagination.Slice(Source, null, out var page);

            Assert.That(items, Is.EqualTo(Source));
            Assert.That(page.Offset, Is.EqualTo(0));
            Assert.That(page.Limit, Is.EqualTo(AgentPagination.DefaultLimit));
            Assert.That(page.Total, Is.EqualTo(5));
            Assert.That(page.HasMore, Is.False);
        }

        [Test]
        public void ReportsHasMoreForPartialPages()
        {
            var args = new AgentPageArgs { Offset = 1, Limit = 2 };

            var items = AgentPagination.Slice(Source, args, out var page);

            Assert.That(items, Is.EqualTo(new List<int> { 2, 3 }));
            Assert.That(page.HasMore, Is.True);
            Assert.That(page.Total, Is.EqualTo(5));
        }

        [Test]
        public void ValidatesLimitBounds()
        {
            Assert.That(AgentPagination.Validate(null), Is.Null);
            Assert.That(AgentPagination.Validate(new AgentPageArgs { Limit = 1000 }), Is.Null);

            var tooBig = AgentPagination.Validate(new AgentPageArgs { Limit = 1001 });
            Assert.That(tooBig.Code, Is.EqualTo(AgentErrorCodes.InvalidArgument));

            var negative = AgentPagination.Validate(new AgentPageArgs { Offset = -1 });
            Assert.That(negative.Code, Is.EqualTo(AgentErrorCodes.InvalidArgument));
        }
    }
}

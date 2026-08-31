using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;
using UnityEditor;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class AgentTokenStoreTest
    {
        private const string SessionKey = "UnityAgentFramework.BearerToken";
        private string _savedToken;

        [SetUp]
        public void SetUp()
        {
            _savedToken = SessionState.GetString(SessionKey, string.Empty);
            AgentTokenStore.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (string.IsNullOrEmpty(_savedToken))
            {
                AgentTokenStore.ClearForTests();
            }
            else
            {
                SessionState.SetString(SessionKey, _savedToken);
            }
        }

        [Test]
        public void GeneratesStableSixtyFourCharacterHexToken()
        {
            var token = AgentTokenStore.GetOrCreate();

            Assert.That(token, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(AgentTokenStore.GetOrCreate(), Is.EqualTo(token));
        }

        [Test]
        public void ComparesTokensInConstantTimeSemantics()
        {
            Assert.That(AgentTokenStore.ConstantTimeEquals("abcd", "abcd"), Is.True);
            Assert.That(AgentTokenStore.ConstantTimeEquals("abcd", "abce"), Is.False);
            Assert.That(AgentTokenStore.ConstantTimeEquals("abcd", "abc"), Is.False);
            Assert.That(AgentTokenStore.ConstantTimeEquals(null, "abcd"), Is.False);
            Assert.That(AgentTokenStore.ConstantTimeEquals("abcd", null), Is.False);
        }
    }
}

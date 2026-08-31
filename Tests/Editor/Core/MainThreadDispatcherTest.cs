using System.Threading;
using System.Threading.Tasks;
using com.amari_noa.unity_agent_framework.core.editor;
using NUnit.Framework;

namespace com.amari_noa.unity_agent_framework.core.editor.tests
{
    public class MainThreadDispatcherTest
    {
        [Test]
        public void RunsPostedWorkOnPumpingThreadInArrivalOrder()
        {
            var pumpThreadId = Thread.CurrentThread.ManagedThreadId;
            Task<int> first = null;
            Task<int> second = null;

            var poster = new Thread(() =>
            {
                first = MainThreadDispatcher.RunAsync(() => Thread.CurrentThread.ManagedThreadId);
                second = MainThreadDispatcher.RunAsync(() => 42);
            });
            poster.Start();
            poster.Join();

            Assert.That(first.IsCompleted, Is.False);

            MainThreadDispatcher.PumpOnce();

            Assert.That(first.Result, Is.EqualTo(pumpThreadId));
            Assert.That(second.Result, Is.EqualTo(42));
        }

        [Test]
        public void PropagatesExceptionsToTheCaller()
        {
            var task = MainThreadDispatcher.RunAsync<int>(() => throw new System.InvalidOperationException("boom"));

            MainThreadDispatcher.PumpOnce();

            Assert.That(task.IsFaulted, Is.True);
            var inner = task.Exception.InnerException;
            Assert.That(inner, Is.TypeOf<System.InvalidOperationException>());
            Assert.That(inner.Message, Is.EqualTo("boom"));
        }
    }
}

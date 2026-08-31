#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace com.amari_noa.unity_agent_framework.core.editor
{
    /// <summary>
    /// Serial main-thread dispatcher (design doc section 21). Work posted from
    /// background threads (HTTP handlers) is executed in arrival order on the
    /// editor main thread via EditorApplication.update.
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        /// <summary>Run a function on the main thread and observe its result.</summary>
        public static Task<T> RunAsync<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Queue.Enqueue(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Execute all currently queued items. Called from EditorApplication.update
        /// by the bootstrap, and directly by edit mode tests.
        /// </summary>
        public static void PumpOnce()
        {
            while (Queue.TryDequeue(out var action))
            {
                action();
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace RhinoCodePlatform.Rhino3D.Testing
{
    public sealed class Dispatcher
    {
        sealed class DispatcherSyncContext : SynchronizationContext
        {
            readonly Dispatcher _disp;
            public DispatcherSyncContext(Dispatcher disp) => _disp = disp;
            public override void Post(SendOrPostCallback d, object? state)
            {
                _disp.EnqueueAction(() => d(state));
            }
        }

        readonly Thread _t;
        readonly BlockingCollection<Action> _queue = new();

        public Dispatcher()
        {
            _t = new(Execute) { IsBackground = true };
            _t.Start(this);
        }

        public Task InvokeAsync(Func<Task> action)
        {
            var dt = new TaskCompletionSource<bool>();

            EnqueueAction(async () =>
            {
                try
                {
                    await action();
                    dt.SetResult(true);
                }
                catch (Exception ex)
                {
                    dt.SetException(ex);
                }
            });

            return dt.Task;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> action)
        {
            var dt = new TaskCompletionSource<TResult>();

            EnqueueAction(async () =>
            {
                try
                {
                    TResult res = await action();
                    dt.SetResult(res);
                }
                catch (Exception ex)
                {
                    dt.SetException(ex);
                }
            });

            return dt.Task;
        }

        public void EnqueueAction(Action action) => _queue.Add(action);

        static void Execute(object dispatcher)
        {
            Dispatcher disp = (Dispatcher)dispatcher!;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSyncContext(disp));
            while (true)
            {
                Action a = disp._queue.Take();
                a();
            }
        }
    }
}

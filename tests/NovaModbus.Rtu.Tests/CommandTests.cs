using NovaModbus.Rtu.Commands;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class CommandTests
{
    private sealed class RecordingContext : SynchronizationContext
    {
        private readonly object _sync = new();
        private readonly List<(SendOrPostCallback Callback, object? State)> _posted = [];

        public override void Post(SendOrPostCallback d, object? state)
        {
            ArgumentNullException.ThrowIfNull(d);
            lock (_sync)
            {
                _posted.Add((d, state));
            }
        }

        public int PostedCount
        {
            get
            {
                lock (_sync)
                {
                    return _posted.Count;
                }
            }
        }

        public void Drain()
        {
            List<(SendOrPostCallback Callback, object? State)> batch;
            lock (_sync)
            {
                batch = [.. _posted];
                _posted.Clear();
            }

            foreach ((SendOrPostCallback callback, object? state) in batch)
            {
                callback(state);
            }
        }
    }

    [TestMethod]
    public void RaiseFromBackgroundThreadMarshalsToContext()
    {
        RecordingContext context = new();
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            RelayCommand command = new(() => { });
            int raised = 0;
            command.CanExecuteChanged += (_, _) => Interlocked.Increment(ref raised);

            // Blocking wait: awaiting here would capture the recording context
            // and deadlock (continuations only run on Drain).
            bool completed = Task.Run(() => command.RaiseCanExecuteChanged())
                .Wait(TimeSpan.FromSeconds(5));
            Assert.IsTrue(completed);
            Assert.AreEqual(0, raised);
            Assert.AreEqual(1, context.PostedCount);
            context.Drain();
            Assert.AreEqual(1, raised);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [TestMethod]
    public void AsyncErrorRoutedThroughContext()
    {
        RecordingContext context = new();
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            InvalidOperationException failure = new("boom");
            Exception? received = null;
            AsyncRelayCommand command = new(
                () => Task.FromException(failure),
                onError: ex => received = ex);

            command.Execute(null);
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (received is null && DateTime.UtcNow < deadline)
            {
                context.Drain();
                Thread.Sleep(10);
            }

            context.Drain();
            Assert.AreSame(failure, received);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [TestMethod]
    public async Task ReentrancyGuardRunsOnce()
    {
        int started = 0;
        TaskCompletionSource gate = new();
        AsyncRelayCommand command = new(async () =>
        {
            Interlocked.Increment(ref started);
            await gate.Task;
        });

        command.Execute(null);
        command.Execute(null);
        await Task.Delay(50);
        Assert.AreEqual(1, Volatile.Read(ref started));
        gate.SetResult();
        for (int i = 0; i < 100 && !command.CanExecute(null); i++)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public void CanExecuteHonorsPredicate()
    {
        bool allowed = false;
        RelayCommand command = new(() => { }, () => allowed);
        Assert.IsFalse(command.CanExecute(null));
        allowed = true;
        Assert.IsTrue(command.CanExecute(null));
    }
}

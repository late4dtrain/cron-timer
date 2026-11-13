using System.Collections.Concurrent;
using Late4dTrain.CronTimer.Providers;

namespace Late4dTrain.CronTimer.Tests.Providers;

public sealed class MockDelayProvider : IDelayProvider
{
    private readonly MockTimeProvider _clock;
    private readonly ConcurrentQueue<PendingDelay> _pending = new();
    private readonly SemaphoreSlim _available = new(0);

    public MockDelayProvider(MockTimeProvider clock) => _clock = clock;
    public int PendingCount => _pending.Count;

    public async Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        var pending = new PendingDelay(delay, cancellationToken);
        _pending.Enqueue(pending);
        _available.Release();
        try { await pending.Completion.Task.ConfigureAwait(false); }
        finally { pending.CancellationRegistration.Dispose(); }
    }

    public async Task<PendingDelay> NextAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await _available.WaitAsync(timeout.Token);
        if (!_pending.TryDequeue(out var pending))
            throw new InvalidOperationException("Delay queue was unexpectedly empty.");
        return pending;
    }

    public void Advance(PendingDelay pending)
    {
        _clock.Advance(pending.Delay);
        pending.Completion.TrySetResult();
    }

    public sealed class PendingDelay
    {
        internal PendingDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delay = delay;
            Completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationRegistration = cancellationToken.Register(() => Completion.TrySetCanceled(cancellationToken));
        }

        public TimeSpan Delay { get; }
        internal TaskCompletionSource Completion { get; }
        internal CancellationTokenRegistration CancellationRegistration { get; }
    }
}

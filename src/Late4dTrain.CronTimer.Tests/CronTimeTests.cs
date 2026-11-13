using System.Collections.Concurrent;
using FluentAssertions;
using Late4dTrain.CronTimer.Events;
using Late4dTrain.CronTimer.Options;
using Late4dTrain.CronTimer.Parser;
using Late4dTrain.CronTimer.Providers;
using Late4dTrain.CronTimer.Tests.Providers;

namespace Late4dTrain.CronTimer.Tests;

public class CronTimeTests
{
    private static readonly DateTime StartTime = new(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CronTimer CreateTimer(MockTimeProvider clock, MockDelayProvider delay,
        int timeout = Timeout.Infinite, Action<string, Exception>? errorLogger = null,
        Action<string>? infoLogger = null, params CronTab[] tabs) =>
        new(options => options.AddCronTabs(tabs.Length == 0
                ? new[] { new CronTab("* * * * * *", CronFormats.IncludeSeconds) }
                : tabs),
            clock, delay, timeout, errorLogger, infoLogger);

    [Fact]
    public async Task Start_Should_Schedule_One_Worker_And_Fire_At_The_Expected_Time()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        var triggered = NewSignal<CronEventArgs>();
        timer.TriggeredAsyncEventHandler += (_, args) => { triggered.TrySetResult(args); return Task.CompletedTask; };

        timer.Start();
        timer.Start();
        var pending = await delay.NextAsync();
        delay.PendingCount.Should().Be(0);
        pending.Delay.Should().Be(TimeSpan.FromSeconds(1));
        delay.Advance(pending);
        var args = await triggered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        args.TriggeredUtcDateTime.Should().Be(StartTime.AddSeconds(1));
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_Should_Return_Worker_Lifetime_And_Observe_External_Cancellation()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        using var cts = new CancellationTokenSource();

        var worker = timer.StartAsync(cts.Token);
        await delay.NextAsync();
        cts.Cancel();

        await worker.WaitAsync(TimeSpan.FromSeconds(2));
        timer.Start();
        await delay.NextAsync();
        await timer.StopAsync(CancellationToken.None);
        timer.Stop();
    }

    [Fact]
    public async Task StartAsync_With_PreCanceled_Token_Should_Not_Start_Work()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await timer.StartAsync(cts.Token).WaitAsync(TimeSpan.FromSeconds(2));
        delay.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task StopAsync_Should_Cancel_Pending_Delay_And_Wait_For_Worker()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        timer.Start();
        await delay.NextAsync();

        await timer.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        delay.PendingCount.Should().Be(0);
        timer.Start();
        await delay.NextAsync();
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_Should_Wait_For_Active_Async_Callback()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        var entered = NewSignal();
        var release = NewSignal();
        timer.TriggeredAsyncEventHandler += async (_, args) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(args.CancellationToken);
        };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        delay.PendingCount.Should().Be(0);
        var stopping = timer.StopAsync(CancellationToken.None);
        stopping.IsCompleted.Should().BeFalse();
        release.TrySetResult();
        await stopping.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task StopAsync_Canceled_Wait_Should_Not_Dispose_Worker_Resources()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        var entered = NewSignal();
        var release = NewSignal();
        timer.TriggeredAsyncEventHandler += async (_, _) =>
        {
            entered.TrySetResult();
            await release.Task;
        };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using var waitCts = new CancellationTokenSource();
        var stopping = timer.StopAsync(waitCts.Token);
        waitCts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopping);

        release.TrySetResult();
        await timer.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Timeout_Should_Cancel_Callback_Token_And_Log_It()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var info = new ConcurrentQueue<string>();
        using var timer = CreateTimer(clock, delay, 100, infoLogger: message => info.Enqueue(message));
        var observedCancellation = NewSignal();
        var calls = 0;
        var nextOccurrence = NewSignal();
        timer.TriggeredAsyncEventHandler += async (_, args) =>
        {
            if (Interlocked.Increment(ref calls) > 1)
            {
                nextOccurrence.TrySetResult();
                return;
            }
            try { await Task.Delay(Timeout.Infinite, args.CancellationToken); }
            catch (OperationCanceledException) { observedCancellation.TrySetResult(); }
        };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await observedCancellation.Task.WaitAsync(TimeSpan.FromSeconds(2));
        info.Should().Contain(message => message.Contains("timed out", StringComparison.OrdinalIgnoreCase));
        delay.Advance(await delay.NextAsync());
        await nextOccurrence.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await timer.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Async_Subscribers_Should_Be_Awaited_In_Registration_Order()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        var entered = NewSignal();
        var release = NewSignal();
        var secondCalled = NewSignal();
        timer.TriggeredAsyncEventHandler += async (_, _) =>
        {
            entered.TrySetResult();
            await release.Task;
        };
        timer.TriggeredAsyncEventHandler += (_, _) => { secondCalled.TrySetResult(); return Task.CompletedTask; };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        secondCalled.Task.IsCompleted.Should().BeFalse();
        release.TrySetResult();
        await secondCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Tied_Schedules_Should_Fire_In_Configuration_Order_With_Their_Metadata()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var first = new CronTab("* * * * * *", CronFormats.IncludeSeconds);
        var second = new CronTab("* * * * * *", CronFormats.IncludeSeconds);
        using var timer = CreateTimer(clock, delay, tabs: [first, second]);
        var events = new ConcurrentQueue<CronEventArgs>();
        var both = NewSignal();
        timer.TriggeredAsyncEventHandler += (_, args) =>
        {
            events.Enqueue(args);
            if (events.Count == 2) both.TrySetResult();
            return Task.CompletedTask;
        };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await both.Task.WaitAsync(TimeSpan.FromSeconds(2));
        events.Select(args => args.Id).Should().Equal(first.Id, second.Id);
        events.Select(args => args.Expression).Should().Equal(first.Expression, second.Expression);
        events.Select(args => args.TriggeredUtcDateTime).Should().OnlyContain(time => time == StartTime.AddSeconds(1));
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Earliest_Schedule_Should_Win_Regardless_Of_Configuration_Order()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var later = new CronTab("*/3 * * * * *", CronFormats.IncludeSeconds);
        var earlier = new CronTab("*/2 * * * * *", CronFormats.IncludeSeconds);
        using var timer = CreateTimer(clock, delay, tabs: [later, earlier]);
        var firstEvent = NewSignal<CronEventArgs>();
        timer.TriggeredAsyncEventHandler += (_, args) => { firstEvent.TrySetResult(args); return Task.CompletedTask; };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        var args = await firstEvent.Task.WaitAsync(TimeSpan.FromSeconds(2));
        args.Id.Should().Be(earlier.Id);
        args.TriggeredUtcDateTime.Should().Be(StartTime.AddSeconds(2));
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Callback_Failure_Should_Log_And_Continue_To_Next_Occurrence()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var errors = new ConcurrentQueue<Exception>();
        using var timer = CreateTimer(clock, delay, errorLogger: (_, exception) => errors.Enqueue(exception));
        var calls = 0;
        var recovered = NewSignal();
        timer.TriggeredAsyncEventHandler += (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1) throw new InvalidOperationException("expected");
            recovered.TrySetResult();
            return Task.CompletedTask;
        };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        delay.Advance(await delay.NextAsync());
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        errors.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>();
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Synchronous_And_Asynchronous_Callback_Failures_Should_Be_Logged_Without_Hiding_Later_Handlers()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var errors = new ConcurrentQueue<Exception>();
        using var timer = CreateTimer(clock, delay, errorLogger: (_, exception) => errors.Enqueue(exception));
        var laterSyncHandler = NewSignal();
        var laterAsyncHandler = NewSignal();
        timer.TriggeredEventHandler += (_, _) => throw new InvalidOperationException("sync failure");
        timer.TriggeredEventHandler += (_, _) => laterSyncHandler.TrySetResult();
        timer.TriggeredAsyncEventHandler += async (_, _) =>
        {
            await Task.Yield();
            throw new ApplicationException("async failure");
        };
        timer.TriggeredAsyncEventHandler += (_, _) => { laterAsyncHandler.TrySetResult(); return Task.CompletedTask; };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await laterSyncHandler.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await laterAsyncHandler.Task.WaitAsync(TimeSpan.FromSeconds(2));
        errors.Should().HaveCount(2);
        errors.Should().ContainSingle(exception => exception is InvalidOperationException);
        errors.Should().ContainSingle(exception => exception is ApplicationException);
        await timer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Empty_Schedules_Should_Complete_Without_Spinning()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = new CronTimer(_ => { }, clock, delay);
        await timer.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        delay.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task Impossible_Schedule_Should_Complete_Without_Spinning()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay, tabs: [new CronTab("0 0 31 2 *", CronFormats.Standard)]);
        await timer.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        delay.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task Delay_Provider_Failure_Should_Log_Once_And_Allow_Restart()
    {
        var errors = new ConcurrentQueue<Exception>();
        var delay = new ThrowingDelayProvider();
        var timer = new CronTimer(options => options.AddCronTabs(new CronTab("* * * * * *", CronFormats.IncludeSeconds)),
            new MockTimeProvider(StartTime), delay, errorLogger: (_, exception) => errors.Enqueue(exception));

        await timer.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        errors.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>();
        await timer.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        errors.Should().HaveCount(2);
        timer.Dispose();
    }

    [Fact]
    public async Task No_Subscribers_Should_Not_Prevent_Scheduling_Or_Shutdown()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await timer.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Dispose_Should_Be_Idempotent_And_Reject_New_Starts()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var timer = CreateTimer(clock, delay);
        timer.Dispose();
        timer.Dispose();
        timer.Stop();
        Assert.Throws<ObjectDisposedException>(() => timer.Start());
    }

    [Fact]
    public void Constructor_Should_Reject_Null_Configuration_And_Invalid_Timeout()
    {
        Action nullConfiguration = () => new CronTimer((Action<CronOption>)null!);
        Action invalidTimeout = () => new CronTimer(_ => { }, executionTimeout: -2);
        nullConfiguration.Should().Throw<ArgumentNullException>();
        invalidTimeout.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Callback_Cannot_Await_Its_Own_Stop()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        var errors = new ConcurrentQueue<Exception>();
        using var timer = CreateTimer(clock, delay, errorLogger: (_, exception) => errors.Enqueue(exception));
        timer.TriggeredAsyncEventHandler += async (_, _) => await timer.StopAsync(CancellationToken.None);

        var worker = timer.StartAsync(CancellationToken.None);
        delay.Advance(await delay.NextAsync());
        await worker.WaitAsync(TimeSpan.FromSeconds(2));
        errors.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Sync_Callback_Can_Request_Stop_Without_Blocking_On_Itself()
    {
        var clock = new MockTimeProvider(StartTime);
        var delay = new MockDelayProvider(clock);
        using var timer = CreateTimer(clock, delay);
        var called = NewSignal();
        timer.TriggeredEventHandler += (_, _) => { called.TrySetResult(); timer.Stop(); };

        timer.Start();
        delay.Advance(await delay.NextAsync());
        await called.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await timer.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static TaskCompletionSource<T> NewSignal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ThrowingDelayProvider : IDelayProvider
    {
        public Task Delay(TimeSpan delay, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("expected delay failure"));
    }

}

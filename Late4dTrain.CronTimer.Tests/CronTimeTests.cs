using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Late4dTrain.CronTimer.Parser;
using Late4dTrain.CronTimer.Providers;
using NSubstitute;

namespace Late4dTrain.CronTimer.Tests;

public class CronTimeTests
{
    [Fact]
    public async Task CronTimer_Should_Trigger_Event_With_NSubstitute_TimeProvider()
    {
        // Arrange
        var events = new List<DateTime>();
        var cronExpression = "*/5 * * * * *"; // Every 5 seconds
        var format = CronFormats.IncludeSeconds;
        var startTime = new DateTime(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // Create a substitute for ITimeProvider
        var timeProvider = Substitute.For<ITimeProvider>();
        var delayProvider = Substitute.For<IDelayProvider>();

        // Initialize currentTime with startTime
        var currentTime = startTime;

        // Set up UtcNow to return currentTime dynamically
        timeProvider.UtcNow.Returns(_ => currentTime);

        // Set up Delay to increment currentTime by delay
        delayProvider.Delay(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var delay = ci.Arg<TimeSpan>();
                currentTime = currentTime.Add(delay);
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            });

        using var cronTimer = new CronTimer(options => { options.AddCronTabs(new CronTab(cronExpression, format)); },
            timeProvider, delayProvider);

        var tcs = new TaskCompletionSource<bool>();

        var expectedEvents = 3;
        cronTimer.TriggeredEventHandler += (_, e) =>
        {
            events.Add(e.TriggeredUtcDateTime);
            if (events.Count >= expectedEvents)
            {
                tcs.SetResult(true);
            }
        };

        // Act
        cronTimer.Start();

        // Wait for the events to be processed
        await tcs.Task;

        cronTimer.Stop();

        // Assert
        events.Should().HaveCount(expectedEvents);
        events[0].Should().Be(startTime.AddSeconds(5)); // At 00:00:05
        events[1].Should().Be(startTime.AddSeconds(10)); // At 00:00:10
        events[2].Should().Be(startTime.AddSeconds(15)); // At 00:00:15
    }

    [Fact]
    public async Task CronTimer_Should_Trigger_Event_With_Simplified_Constructor_TimeProvider()
        {
        // Arrange
        var events = new List<DateTime>();
        var startTime = new DateTime(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // Create a substitute for ITimeProvider
        var timeProvider = Substitute.For<ITimeProvider>();
        var delayProvider = Substitute.For<IDelayProvider>();

        // Initialize currentTime with startTime
        var currentTime = startTime;

        // Set up UtcNow to return currentTime dynamically
        timeProvider.UtcNow.Returns(_ => currentTime);

        // Set up Delay to increment currentTime by delay
        delayProvider.Delay(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var delay = ci.Arg<TimeSpan>();
                currentTime = currentTime.Add(delay);
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            });

        using var cronTimer = new CronTimer("*/5 * * * * *", CronFormats.IncludeSeconds, timeProvider, delayProvider);

        var tcs = new TaskCompletionSource<bool>();

        var expectedEvents = 3;
        cronTimer.TriggeredEventHandler += (_, e) =>
        {
            events.Add(e.TriggeredUtcDateTime);
            if (events.Count >= expectedEvents)
            {
                tcs.SetResult(true);
            }
        };

        // Act
        cronTimer.Start();

        // Wait for the events to be processed
        await tcs.Task;

        cronTimer.Stop();

        // Assert
        events.Should().HaveCount(expectedEvents);
        events[0].Should().Be(startTime.AddSeconds(5)); // At 00:00:05
        events[1].Should().Be(startTime.AddSeconds(10)); // At 00:00:10
        events[2].Should().Be(startTime.AddSeconds(15)); // At 00:00:15
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public void CronTimer_Should_Handle_Concurrent_Start_Stop_Calls()
    {
        // Arrange
        var timer = new CronTimer(options =>
        {
            options.AddCronTabs(new CronTab("*/1 * * * * *", CronFormats.IncludeSeconds));
        });

        var startStopTasks = new List<Task>();
        using var cts = new CancellationTokenSource();

        // Act
        for (int i = 0; i < 10; i++)
        {
            startStopTasks.Add(Task.Run(() => timer.Start(), cts.Token));
            startStopTasks.Add(Task.Run(() => timer.Stop(), cts.Token));
        }

#pragma warning disable xUnit1031
        Task.WaitAll(startStopTasks.ToArray(), cts.Token);
#pragma warning restore xUnit1031

        // Assert
        // No exceptions should occur, and the timer's state should be consistent
        timer.Dispose();

        timer.Should().NotBeNull();
    }


    [Fact]
    public void CronTimer_Should_Not_Throw_When_Stop_Called_After_Dispose()
    {
        // Arrange
        var timer = new CronTimer(options =>
        {
            options.AddCronTabs(new CronTab("*/1 * * * * *", CronFormats.IncludeSeconds));
        });

        // Act
        timer.Dispose();

        // Assert
        timer.Stop(); // Should not throw any exceptions

        timer.Should().NotBeNull();
    }
}

using FluentAssertions;
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
        var format = CronExpressionType.IncludeSeconds;
        var startTime = new DateTime(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // Create a substitute for ITimeProvider
        var timeProvider = Substitute.For<ITimeProvider>();
        var delayProvider = Substitute.For<IDelayProvider>();

        // Initialize currentTime with startTime
        var currentTime = startTime;

        // Set up UtcNow to return currentTime
        timeProvider.UtcNow.Returns(
            currentTime,
            currentTime.AddSeconds(5),
            currentTime.AddSeconds(10),
            currentTime.AddSeconds(15),
            currentTime.AddSeconds(20));

        // Set up Delay to increment currentTime by delay
        delayProvider.Delay(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.CompletedTask);

        using var cronTimer = new CronTimer(options => { options.AddCronTabs(new CronTab(cronExpression, format)); },
            timeProvider, delayProvider);

        var tcs = new TaskCompletionSource<bool>();

        var expectedEvents = 3;
        cronTimer.TriggeredEventHandler += (s, e) =>
        {
            events.Add(e.TriggeredUtcDateTime);
            if (events.Count >= expectedEvents)
            {
                tcs.SetResult(true);
            }
        };

        var ct = CancellationToken.None;
        // Act
        cronTimer.Start(ct, executionTimes: 3);

        // Wait for the events to be processed
        await tcs.Task;

        cronTimer.Stop();

        // Assert
        events.Should().HaveCount(expectedEvents);
        events[0].Should().Be(startTime.AddSeconds(5)); // At 00:00:05
        events[1].Should().Be(startTime.AddSeconds(10)); // At 00:00:10
        events[2].Should().Be(startTime.AddSeconds(15)); // At 00:00:15
    }
}

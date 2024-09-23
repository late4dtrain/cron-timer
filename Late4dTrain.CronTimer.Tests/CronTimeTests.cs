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

        using var cronTimer = new CronTimer(options => { options.AddCronTab(new CronTab(cronExpression, format)); },
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
            options.AddCronTab(new CronTab("*/1 * * * * *", CronFormats.IncludeSeconds));
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
            options.AddCronTab(new CronTab("*/1 * * * * *", CronFormats.IncludeSeconds));
        });

        // Act
        timer.Dispose();

        // Assert
        timer.Stop(); // Should not throw any exceptions

        timer.Should().NotBeNull();
    }

    [Fact]
    public async Task CronTimer_Should_Respect_ExecutionTimeout()
    {
        // Arrange
        var events = new List<DateTime>();
        var cronExpression = "0 0 * * *"; // Every day at midnight
        var format = CronFormats.Standard;
        var startTime = new DateTime(2023, 10, 1, 23, 59, 59, DateTimeKind.Utc);
        var executionTimeout = 500; // 500 ms timeout

        var timeProvider = Substitute.For<ITimeProvider>();
        var delayProvider = Substitute.For<IDelayProvider>();

        var currentTime = startTime;
        timeProvider.UtcNow.Returns(_ => currentTime);

        delayProvider.Delay(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var delay = ci.Arg<TimeSpan>();
                currentTime = currentTime.Add(delay);
                await Task.Delay(10); // Short delay to allow for timeout
            });

        using var cronTimer = new CronTimer(
            options => { options.AddCronTab(new CronTab(cronExpression, format)); },
            timeProvider,
            delayProvider,
            executionTimeout);

        var eventTriggered = false;
        cronTimer.TriggeredEventHandler += async (_, e) =>
        {
            if (!eventTriggered)
            {
                eventTriggered = true;
                events.Add(e.TriggeredUtcDateTime);
                await Task.Delay(1000, e.CancellationToken); // Simulate long-running operation
            }
        };

        // Act
        cronTimer.Start();

        // Wait for more than the execution timeout
        await Task.Delay(1500);

        cronTimer.Stop();

        // Assert
        eventTriggered.Should().BeTrue(); // The event should have been triggered
        events.Should().HaveCount(1); // One event should have been added before the timeout
        events[0].Should()
            .Be(new DateTime(2023, 10, 2, 0, 0, 0, DateTimeKind.Utc)); // The event should be at the next day's midnight
    }

    [Fact]
    public async Task CronTimer_Should_Allow_Long_Running_Tasks_With_Infinite_Timeout()
    {
        // Arrange
        var events = new List<DateTime>();
        var cronExpression = "*/1 * * * * *"; // Every second
        var format = CronFormats.IncludeSeconds;
        var startTime = new DateTime(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var timeProvider = Substitute.For<ITimeProvider>();
        var delayProvider = Substitute.For<IDelayProvider>();

        var currentTime = startTime;
        timeProvider.UtcNow.Returns(_ => currentTime);

        delayProvider.Delay(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var delay = ci.Arg<TimeSpan>();
                currentTime = currentTime.Add(delay);
                await Task.Delay(10);
            });

        using var cronTimer = new CronTimer(
            options => { options.AddCronTab(new CronTab(cronExpression, format)); },
            timeProvider,
            delayProvider);

        var tcs = new TaskCompletionSource<bool>();

        cronTimer.TriggeredEventHandler += async (_, e) =>
        {
            await Task.Delay(1000); // Simulate long-running operation
            events.Add(e.TriggeredUtcDateTime);
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(true);
            }
        };

        // Act
        cronTimer.Start();

        // Wait for the event to be processed
        await Task.WhenAny(tcs.Task, Task.Delay(2000));

        cronTimer.Stop();

        // Assert
        events.Should().HaveCount(1); // The event should have completed
        events[0].Should().Be(startTime.AddSeconds(1)); // At 00:00:01
    }


    [Theory]
    [InlineData("0 0 12 * * ?", CronFormats.IncludeSeconds)]
    [InlineData("0 12 * * ?", CronFormats.Standard)]
    [InlineData("*/5 * * * * *", CronFormats.IncludeSeconds)] // Every 5 seconds
    [InlineData("0 0/5 * * * ?", CronFormats.IncludeSeconds)] // Every 5 minutes
    [InlineData("0 0 0/1 * * ?", CronFormats.IncludeSeconds)] // Every hour
    [InlineData("0 0 12 1/1 * ?", CronFormats.IncludeSeconds)] // Every day at noon
    [InlineData("0 0 12 1 1 ?", CronFormats.IncludeSeconds)] // Every year on January 1st at noon
    [InlineData("0 0 12 ? * 2-6", CronFormats.IncludeSeconds)] // Every weekday at noon
    [InlineData("0 0 12 L * ?", CronFormats.IncludeSeconds)] // Last day of every month at noon
    [InlineData("0 0 12 LW * ?", CronFormats.IncludeSeconds)] // Last weekday of every month at noon
    [InlineData("0 0 12 ? * 5L", CronFormats.IncludeSeconds)] // Last Friday of every month at noon
    [InlineData("0 0 12 ? * 5#3", CronFormats.IncludeSeconds)] // Third Friday of every month at noon
    [InlineData("0 0 12 15W * ?", CronFormats.IncludeSeconds)] // Nearest weekday to the 15th of every month at noon
    [InlineData("0 0 12 1W * ?", CronFormats.IncludeSeconds)] // Nearest weekday to the 1st of every month at noon
    [InlineData("0 0 12 ? * MON-FRI", CronFormats.IncludeSeconds)] // Every weekday at noon
    [InlineData("0 0 12 1 1/2 ?", CronFormats.IncludeSeconds)] // Every 2 months on the 1st at noon
    [InlineData("0 0 12 1 1/3 ?", CronFormats.IncludeSeconds)] // Every 3 months on the 1st at noon
    [InlineData("0 0 12 1 1/4 ?", CronFormats.IncludeSeconds)] // Every 4 months on the 1st at noon
    [InlineData("0 0 12 1 1/6 ?", CronFormats.IncludeSeconds)] // Every 6 months on the 1st at noon
    public void ValidateExpression_Should_Not_Throw_For_Valid_Expressions(string expression, CronFormats formats)
    {
        // Act
        Action act = () => new CronTab(expression, formats);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
// Empty expression
    [InlineData("", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 0.")]
    [InlineData("   ", CronFormats.Standard, "Invalid number of fields in cron expression. Expected 5, got 0.")]
// Too few fields
    [InlineData("0 0 12 * *", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 5.")]
    [InlineData("0 12 * *", CronFormats.Standard, "Invalid number of fields in cron expression. Expected 5, got 4.")]
// Too many fields
    [InlineData("0 0 12 * * * *", CronFormats.IncludeSeconds,
        "Invalid number of fields in cron expression. Expected 6, got 7.")]
    [InlineData("0 0 12 * * *", CronFormats.Standard,
        "Invalid number of fields in cron expression. Expected 5, got 6.")]
// Invalid characters
    [InlineData("0 0 12 * * %", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 * * ?", CronFormats.Standard, "Invalid number of fields in cron expression. Expected 5, got 6.")]
// Invalid ranges
    [InlineData("0 0 60 * * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 70 12 * * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 * 13 ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Invalid step values
    [InlineData("*/-5 * * * * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("*/0 * * * * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Invalid day of week
    [InlineData("0 0 12 ? * 8", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 ? * MON-FR", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Invalid day of month
    [InlineData("0 0 12 32 * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 0 * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Missing required fields
    [InlineData("0 0 * * ?", CronFormats.IncludeSeconds,
        "Invalid number of fields in cron expression. Expected 6, got 5.")]
    [InlineData("0 * * ?", CronFormats.Standard, "Invalid number of fields in cron expression. Expected 5, got 4.")]
// Invalid use of 'L', 'W', '#'
    [InlineData("0 0 12 L-3 * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 15#5 * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Invalid month abbreviations
    [InlineData("0 0 12 * JANUARY ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 * JAN-MARCH ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Invalid day of week abbreviations
    [InlineData("0 0 12 ? * MONDAY", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 ? * SUN-SAT", CronFormats.IncludeSeconds, "Invalid day of week value 'SUN-SAT'.")]
// Invalid format specifiers
    [InlineData("0 0 12 * *", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 5.")]
// Non-numeric where numbers are expected
    [InlineData("0 0 twelve * * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 zero 12 * * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
// Invalid use of '/'
    [InlineData("0 0 12 */* * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 *//* ?", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 5.")]
// Invalid use of ','
    [InlineData("0 0 12 * *, ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 * ,*", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 5.")]
// Invalid wildcard usage
    [InlineData("0 0 12 ** * ?", CronFormats.IncludeSeconds, "Invalid cron expression.")]
    [InlineData("0 0 12 * **", CronFormats.IncludeSeconds, "Invalid number of fields in cron expression. Expected 6, got 5.")]
    public void ValidateExpression_Should_Throw_For_Invalid_Expressions(string expression, CronFormats formats,
        string expectedMessage)
    {
        // Act
        Action act = () => new CronTab(expression, formats);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage(expectedMessage);
    }
}

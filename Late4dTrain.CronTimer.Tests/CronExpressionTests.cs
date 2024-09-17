using FluentAssertions;
using Late4dTrain.CronTimer.Parser;

namespace Late4dTrain.CronTimer.Tests
{
    public class CronExpressionTests
    {
        [Theory]
        [InlineData("*/5 * * * * *", CronFormats.IncludeSeconds,
            new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 })]
        [InlineData("0-30/10 * * * * *", CronFormats.IncludeSeconds, new[] { 0, 10, 20, 30 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All seconds
        public void Parse_Should_Parse_Seconds_Correctly(string expression, CronFormats formats,
            int[]? expectedSeconds)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedSeconds != null)
            {
                cronExpression.Seconds.Should().BeEquivalentTo(expectedSeconds);
            }
            else
            {
                cronExpression.Seconds.Should().HaveCount(60).And.ContainInOrder(GenerateSequence(0, 59));
            }
        }

        [Theory]
        [InlineData("* */15 * * * *", CronFormats.IncludeSeconds, new[] { 0, 15, 30, 45 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All minutes
        public void Parse_Should_Parse_Minutes_Correctly(string expression, CronFormats formats,
            int[]? expectedMinutes)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedMinutes != null)
            {
                cronExpression.Minutes.Should().BeEquivalentTo(expectedMinutes);
            }
            else
            {
                cronExpression.Minutes.Should().HaveCount(60).And.ContainInOrder(GenerateSequence(0, 59));
            }
        }

        [Theory]
        [InlineData("* * 0-6 * * *", CronFormats.IncludeSeconds, new[] { 0, 1, 2, 3, 4, 5, 6 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All hours
        public void Parse_Should_Parse_Hours_Correctly(string expression, CronFormats formats,
            int[]? expectedHours)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedHours != null)
            {
                cronExpression.Hours.Should().BeEquivalentTo(expectedHours);
            }
            else
            {
                cronExpression.Hours.Should().HaveCount(24).And.ContainInOrder(GenerateSequence(0, 23));
            }
        }

        [Theory]
        [InlineData("* * * * 1-6/2 *", CronFormats.IncludeSeconds, new[] { 1, 3, 5 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All months
        [InlineData("* * * * JAN,MAR,MAY *", CronFormats.IncludeSeconds, new[] { 1, 3, 5 })] // Month names
        public void Parse_Should_Parse_Months_Correctly(string expression, CronFormats formats,
            int[]? expectedMonths)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedMonths != null)
            {
                cronExpression.Month.Should().BeEquivalentTo(expectedMonths);
            }
            else
            {
                cronExpression.Month.Should().HaveCount(12).And.ContainInOrder(GenerateSequence(1, 12));
            }
        }

        [Theory]
        [InlineData("* * * * * 1-5", CronFormats.IncludeSeconds, new[] { 1, 2, 3, 4, 5 })]
        [InlineData("* * * * * *", CronFormats.IncludeSeconds, null)] // All days of week
        [InlineData("* * * * * MON,WED,FRI", CronFormats.IncludeSeconds, new[] { 1, 3, 5 })] // Day names
        public void Parse_Should_Parse_DaysOfWeek_Correctly(string expression, CronFormats formats,
            int[]? expectedDaysOfWeek)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, formats);

            // Assert
            if (expectedDaysOfWeek != null)
            {
                cronExpression.DayOfWeek.Should().BeEquivalentTo(expectedDaysOfWeek);
            }
            else
            {
                cronExpression.DayOfWeek.Should().HaveCount(7).And.ContainInOrder(GenerateSequence(0, 6));
            }
        }

        [Theory]
        [InlineData("*/5 * * * * *", CronFormats.IncludeSeconds, "2023-10-01T00:00:00Z", "2023-10-01T00:00:05Z")]
        [InlineData("0 */1 * * * *", CronFormats.IncludeSeconds, "2023-10-01T00:59:59Z", "2023-10-01T01:00:00Z")]
        [InlineData("0 0 * * *", CronFormats.Standard, "2023-10-01T23:59:59Z", "2023-10-02T00:00:00Z")]
        public void GetNextOccurrence_Should_Return_Correct_Next_Occurrence(string expression,
            CronFormats formats, DateTimeOffset baseTime, DateTimeOffset expectedTime)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, formats);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime.UtcDateTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime.UtcDateTime);
        }

        [Theory]
        [InlineData("* * L * *", CronFormats.Standard, "2023-01-30T23:59:59Z",
            "2023-01-31T00:00:00Z")] // Last day of the month
        [InlineData("* * * * 6L", CronFormats.Standard, "2023-01-27T23:59:59Z",
            "2023-01-28T00:00:00Z")] // Last Saturday
        public void GetNextOccurrence_Should_Handle_Special_Operators_Correctly(string expression,
            CronFormats formats,
            DateTimeOffset baseTime, DateTimeOffset expectedTime)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, formats);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime.UtcDateTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime.UtcDateTime);
        }

        [Theory]
        [InlineData("* * * * * *", CronFormats.Standard)] // Incorrect number of fields
        [InlineData("* * * * * * * *", CronFormats.IncludeSeconds)] // Too many fields
        [InlineData("*/0 * * * * *", CronFormats.IncludeSeconds)] // Invalid step value
        public void Parse_Should_Throw_Exception_On_Invalid_Expressions(string expression, CronFormats formats)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CronExpression.Parse(expression, formats));
        }

        private static int[] GenerateSequence(int start, int end)
        {
            int length = end - start + 1;
            int[] sequence = new int[length];
            for (int i = 0; i < length; i++)
            {
                sequence[i] = start + i;
            }

            return sequence;
        }
    }
}

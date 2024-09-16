using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Late4dTrain.CronTimer.Tests
{
    public class CronExpressionTests
    {
        [Theory]
        [InlineData("*/5 * * * * *", CronExpressionType.IncludeSeconds,
            new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 })]
        [InlineData("0-30/10 * * * * *", CronExpressionType.IncludeSeconds, new[] { 0, 10, 20, 30 })]
        [InlineData("* * * * * *", CronExpressionType.IncludeSeconds, null)] // All seconds
        public void Parse_Should_Parse_Seconds_Correctly(string expression, CronExpressionType format,
            int[] expectedSeconds)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* */15 * * * *", CronExpressionType.IncludeSeconds, new[] { 0, 15, 30, 45 })]
        [InlineData("* * * * * *", CronExpressionType.IncludeSeconds, null)] // All minutes
        public void Parse_Should_Parse_Minutes_Correctly(string expression, CronExpressionType format,
            int[] expectedMinutes)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* * 0-6 * * *", CronExpressionType.IncludeSeconds, new[] { 0, 1, 2, 3, 4, 5, 6 })]
        [InlineData("* * * * * *", CronExpressionType.IncludeSeconds, null)] // All hours
        public void Parse_Should_Parse_Hours_Correctly(string expression, CronExpressionType format,
            int[] expectedHours)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* * * * 1-6/2 *", CronExpressionType.IncludeSeconds, new[] { 1, 3, 5 })]
        [InlineData("* * * * * *", CronExpressionType.IncludeSeconds, null)] // All months
        [InlineData("* * * * JAN,MAR,MAY *", CronExpressionType.IncludeSeconds, new[] { 1, 3, 5 })] // Month names
        public void Parse_Should_Parse_Months_Correctly(string expression, CronExpressionType format,
            int[] expectedMonths)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("* * * * * 1-5", CronExpressionType.IncludeSeconds, new[] { 1, 2, 3, 4, 5 })]
        [InlineData("* * * * * *", CronExpressionType.IncludeSeconds, null)] // All days of week
        [InlineData("* * * * * MON,WED,FRI", CronExpressionType.IncludeSeconds, new[] { 1, 3, 5 })] // Day names
        public void Parse_Should_Parse_DaysOfWeek_Correctly(string expression, CronExpressionType format,
            int[] expectedDaysOfWeek)
        {
            // Act
            var cronExpression = CronExpression.Parse(expression, format);

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
        [InlineData("*/5 * * * * *", CronExpressionType.IncludeSeconds, "2023-10-01T00:00:00Z", "2023-10-01T00:00:05Z")]
        [InlineData("0 */1 * * * *", CronExpressionType.IncludeSeconds, "2023-10-01T00:59:59Z", "2023-10-01T01:00:00Z")]
        [InlineData("0 0 * * *", CronExpressionType.Standard, "2023-10-01T23:59:59Z", "2023-10-02T00:00:00Z")]
        public void GetNextOccurrence_Should_Return_Correct_Next_Occurrence(string expression,
            CronExpressionType format, string baseTimeString, string expectedTimeString)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, format);
            var baseTime = DateTime.Parse(baseTimeString, null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal);
            var expectedTime = DateTime.Parse(expectedTimeString, null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime);
        }

        [Theory]
        [InlineData("* * L * *", CronExpressionType.Standard, "2023-01-30T23:59:59Z",
            "2023-01-31T00:00:00Z")] // Last day of the month
        [InlineData("* * * * 6L", CronExpressionType.Standard, "2023-01-27T23:59:59Z",
            "2023-01-28T00:00:00Z")] // Last Saturday
        public void GetNextOccurrence_Should_Handle_Special_Operators_Correctly(string expression,
            CronExpressionType format,
            string baseTimeString, string expectedTimeString)
        {
            // Arrange
            var cronExpression = CronExpression.Parse(expression, format);
            var baseTime = DateTime.Parse(baseTimeString, null,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal);
            DateTime? expectedTime = string.IsNullOrEmpty(expectedTimeString)
                ? (DateTime?)null
                : DateTime.Parse(expectedTimeString, null,
                    System.Globalization.DateTimeStyles.AssumeUniversal |
                    System.Globalization.DateTimeStyles.AdjustToUniversal);

            // Act
            var nextOccurrence = cronExpression.GetNextOccurrence(baseTime);

            // Assert
            nextOccurrence.Should().Be(expectedTime);
        }

        [Theory]
        [InlineData("* * * * * *", CronExpressionType.Standard)] // Incorrect number of fields
        [InlineData("* * * * * * * *", CronExpressionType.IncludeSeconds)] // Too many fields
        [InlineData("*/0 * * * * *", CronExpressionType.IncludeSeconds)] // Invalid step value
        public void Parse_Should_Throw_Exception_On_Invalid_Expressions(string expression, CronExpressionType format)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CronExpression.Parse(expression, format));
        }

        private int[] GenerateSequence(int start, int end)
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
